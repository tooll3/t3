using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Graph;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Commands.Graph
{
    public class CopySymbolChildrenCommand : ICommand
    {
        public string Name => "Copy Symbol Children";

        public bool IsUndoable => true;

        public Dictionary<Guid, Guid> OldToNewIdDict { get; } = new();

        private CopyMode _copyMode;
        
        public enum CopyMode {Normal, ClipboardSource, ClipboardTarget}
        
        private readonly Action? _destructorAction;

        public CopySymbolChildrenCommand(SymbolUi sourceCompositionUi,
                                         IEnumerable<SymbolUi.Child> symbolChildrenToCopy,
                                         List<Annotation> selectedAnnotations,
                                         SymbolUi targetCompositionUi,
                                         Vector2 targetPosition, CopyMode copyMode = CopyMode.Normal, Symbol sourceSymbol = null)
        {
            _copyMode = copyMode;
            
            if (copyMode == CopyMode.ClipboardSource)
            {
                _clipboardSymbolUi = sourceCompositionUi;
                _sourcePastedSymbol = sourceSymbol;
                _sourceSymbolId = sourceSymbol!.Id;
            }
            else
            {
                _sourceSymbolId = sourceCompositionUi.Symbol.Id;
                sourceSymbol = sourceCompositionUi.Symbol;
            }
            
            _targetSymbolId = targetCompositionUi.Symbol.Id;
            
            if (copyMode == CopyMode.ClipboardTarget)
            {
                _clipboardSymbolUi = targetCompositionUi;
                //_destructorAction = () => ((EditorSymbolPackage)targetCompositionUi.Symbol.SymbolPackage).RemoveSymbolUi(targetCompositionUi);
            }
            
            _targetPosition = targetPosition;

            symbolChildrenToCopy ??= sourceCompositionUi.ChildUis.Values.ToArray();

            Vector2 upperLeftCorner = new Vector2(Single.MaxValue, Single.MaxValue);
            foreach (var childToCopy in symbolChildrenToCopy)
            {
                upperLeftCorner = Vector2.Min(upperLeftCorner, childToCopy.PosOnCanvas);
            }

            PositionOffset = targetPosition - upperLeftCorner;

            foreach (var childToCopy in symbolChildrenToCopy)
            {
                Entry entry = new Entry(childToCopy.Id, Guid.NewGuid(), childToCopy.PosOnCanvas - upperLeftCorner, childToCopy.Size);
                _childrenToCopy.Add(entry);
                OldToNewIdDict.Add(entry.ChildId, entry.AddedId);
            }

            foreach (var entry in _childrenToCopy)
            {
                _connectionsToCopy.AddRange(from con in sourceSymbol.Connections
                                            where con.TargetParentOrChildId == entry.ChildId
                                            let newTargetId = OldToNewIdDict[entry.ChildId]
                                            from connectionSource in symbolChildrenToCopy
                                            where con.SourceParentOrChildId == connectionSource.Id
                                            let newSourceId = OldToNewIdDict[connectionSource.Id]
                                            select new Symbol.Connection(newSourceId, con.SourceSlotId, newTargetId, con.TargetSlotId));
            }

            _connectionsToCopy.Reverse(); // to keep multi input order
            if (selectedAnnotations != null && selectedAnnotations.Count > 0)
            {
                _annotationsToCopy = selectedAnnotations
                                    .Select(a => a.Clone())
                                    .ToList();
                //_annotationsToCopy.AddRange(selectedAnnotations);
            }
        }
        
        ~CopySymbolChildrenCommand()
        {
            _destructorAction?.Invoke();
        }

        public void Undo()
        {
            if(!SymbolUiRegistry.TryGetSymbolUi(_targetSymbolId, out var parentSymbolUi))
            {
                this.LogError(true, $"Failed to find target symbol with id: {_targetSymbolId} - was it removed?");
                return;
            }
            
            foreach (var child in _childrenToCopy)
            {
                parentSymbolUi.RemoveChild(child.AddedId);
            }

            foreach (var annotation in _annotationsToCopy)
            {
                parentSymbolUi.Annotations.Remove(annotation.Id);
            }

            NewSymbolChildIds.Clear();
            parentSymbolUi.FlagAsModified();
        }

        public void Do()
        {
            SymbolUi targetCompositionSymbolUi;
            SymbolUi sourceCompositionSymbolUi;
            Symbol sourceCompositionSymbol;
            
            if (_copyMode == CopyMode.ClipboardTarget)
            {
                targetCompositionSymbolUi = _clipboardSymbolUi;
            }
            else if (!SymbolUiRegistry.TryGetSymbolUi(_targetSymbolId, out targetCompositionSymbolUi))
            {
                this.LogError(false, $"Failed to find target symbol with id: {_targetSymbolId} - was it removed?");
                return;
            }


            if (_copyMode == CopyMode.ClipboardSource)
            {
                sourceCompositionSymbolUi = _clipboardSymbolUi;
                sourceCompositionSymbol = _sourcePastedSymbol!;
            }
            else
            {
                if (!SymbolUiRegistry.TryGetSymbolUi(_sourceSymbolId, out sourceCompositionSymbolUi))
                {
                    this.LogError(false, $"Failed to find source symbol with id: {_sourceSymbolId} - was it removed?");
                    return;
                }
                
                sourceCompositionSymbol = sourceCompositionSymbolUi.Symbol;
            }
            
            var targetSymbol = targetCompositionSymbolUi!.Symbol;

            // copy animations first, so when creating the new child instances can automatically create animations actions for the existing curves
            var childIdsToCopyAnimations = _childrenToCopy.Select(entry => entry.ChildId).ToList();
            var oldToNewIdDict = _childrenToCopy.ToDictionary(entry => entry.ChildId, entry => entry.AddedId);
            sourceCompositionSymbol.Animator.CopyAnimationsTo(targetSymbol.Animator, childIdsToCopyAnimations, oldToNewIdDict);

            foreach (var childEntryToCopy in _childrenToCopy)
            {
                if (!sourceCompositionSymbol.Children.TryGetValue(childEntryToCopy.ChildId, out var symbolChildToCopy))
                {
                    Log.Warning("Skipping attempt to copy undefined operator. This can be related to undo/redo operations. Please try to reproduce and tell pixtur");
                    continue;
                }

                var symbolToAdd = symbolChildToCopy.Symbol;
                var newSymbolChild = targetCompositionSymbolUi.AddChildAsCopyFromSource(symbolToAdd, 
                                                                                        symbolChildToCopy, 
                                                                                        sourceCompositionSymbolUi,
                                                                                        _targetPosition + childEntryToCopy.RelativePosition,
                                                                                        childEntryToCopy.AddedId);

                //Symbol.Child newSymbolChild = targetSymbol.Children.Find(child => child.Id == childToCopy.AddedId);
                NewSymbolChildIds.Add(newSymbolChild.Id);
                var newSymbolInputs = newSymbolChild.Inputs;
                foreach (var (id, input) in symbolChildToCopy.Inputs)
                {
                    var newInput = newSymbolInputs[id];
                    newInput.Value.Assign(input.Value.Clone());
                    newInput.IsDefault = input.IsDefault;
                }

                var newSymbolOutputs = newSymbolChild.Outputs;
                foreach (var (id, output) in symbolChildToCopy.Outputs)
                {
                    var newOutput = newSymbolOutputs[id];

                    if (output.OutputData != null)
                    {
                        newOutput.OutputData.Assign(output.OutputData);
                    }

                    newOutput.DirtyFlagTrigger = output.DirtyFlagTrigger;
                    newOutput.IsDisabled = output.IsDisabled;
                    
                }
                if(symbolChildToCopy.IsBypassed)
                {
                    newSymbolChild.IsBypassed = true;
                }
            }

            // add connections between copied children
            foreach (var connection in _connectionsToCopy)
            {
                targetCompositionSymbolUi.Symbol.AddConnection(connection);
            }

            foreach (var annotation in _annotationsToCopy)
            {
                targetCompositionSymbolUi.Annotations[annotation.Id] = annotation;
                targetCompositionSymbolUi.Annotations[annotation.Id].PosOnCanvas += PositionOffset;
                NewSymbolAnnotationIds.Add(annotation.Id);
            }
            
            targetCompositionSymbolUi.FlagAsModified();
        }

        public readonly List<Guid> NewSymbolChildIds = new(); //This primarily used for selecting the new children
        public List<Guid> NewSymbolAnnotationIds = new(); //This primarily used for selecting the new children

        struct Entry
        {
            public Entry(Guid childId, Guid addedId, Vector2 relativePosition, Vector2 size)
            {
                ChildId = childId;
                AddedId = addedId;
                RelativePosition = relativePosition;
                Size = size;
            }

            public readonly Guid ChildId;
            public readonly Guid AddedId;
            public readonly Vector2 RelativePosition;
            public readonly Vector2 Size;
        }

        private static void LogError(bool isUndo,string log)
        {
            Log.Warning($"{nameof(CopySymbolChildrenCommand)} {(isUndo ? "Undo" : "Redo")}: {log}");
        }

        private readonly Vector2 _targetPosition;
        private readonly Guid _sourceSymbolId;
        private readonly Symbol? _sourcePastedSymbol;
        private readonly SymbolUi _clipboardSymbolUi;
        private readonly Guid _targetSymbolId;
        private readonly List<Entry> _childrenToCopy = new();
        private readonly List<Annotation> _annotationsToCopy = new();
        private readonly List<Symbol.Connection> _connectionsToCopy = new();
        public Vector2 PositionOffset;
    }
}