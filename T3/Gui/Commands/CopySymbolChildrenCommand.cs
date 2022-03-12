using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui.Commands
{
    public class CopySymbolChildrenCommand : ICommand
    {
        public string Name => "Copy Symbol Children";

        public bool IsUndoable => true;

        public Dictionary<Guid, Guid> OldToNewIdDict { get; } = new Dictionary<Guid, Guid>();

        public CopySymbolChildrenCommand(SymbolUi sourceCompositionUi, IEnumerable<SymbolChildUi> symbolChildrenToCopy, SymbolUi targetCompositionUi,
                                         Vector2 targetPosition)
        {
            _sourceSymbolId = sourceCompositionUi.Symbol.Id;
            _targetSymbolId = targetCompositionUi.Symbol.Id;
            _targetPosition = targetPosition;

            if (symbolChildrenToCopy == null)
            {
                // if no specific children are selected copy all of the source composition op
                symbolChildrenToCopy = sourceCompositionUi.ChildUis;
            }

            Vector2 upperLeftCorner = new Vector2(Single.MaxValue, Single.MaxValue);
            foreach (var childToCopy in symbolChildrenToCopy)
            {
                upperLeftCorner = Vector2.Min(upperLeftCorner, childToCopy.PosOnCanvas);
            }

            PositionOffset =  targetPosition - upperLeftCorner;

            foreach (var childToCopy in symbolChildrenToCopy)
            {
                Entry entry = new Entry(childToCopy.Id, Guid.NewGuid(), childToCopy.PosOnCanvas - upperLeftCorner, childToCopy.Size);
                _childrenToCopy.Add(entry);
                OldToNewIdDict.Add(entry.ChildId, entry.AddedId);
            }

            foreach (var entry in _childrenToCopy)
            {
                _connectionsToCopy.AddRange(from con in sourceCompositionUi.Symbol.Connections
                                            where con.TargetParentOrChildId == entry.ChildId
                                            let newTargetId = OldToNewIdDict[entry.ChildId]
                                            from connectionSource in symbolChildrenToCopy
                                            where con.SourceParentOrChildId == connectionSource.Id
                                            let newSourceId = OldToNewIdDict[connectionSource.Id]
                                            select new Symbol.Connection(newSourceId, con.SourceSlotId, newTargetId, con.TargetSlotId));
            }

            _connectionsToCopy.Reverse(); // to keep multi input order
        }

        public void Undo()
        {
            var parentSymbolUi = SymbolUiRegistry.Entries[_targetSymbolId];
            foreach (var child in _childrenToCopy)
            {
                parentSymbolUi.RemoveChild(child.AddedId);
            }
            NewSymbolChildIds.Clear();
        }

        public void Do()
        {
            var targetCompositionSymbolUi = SymbolUiRegistry.Entries[_targetSymbolId];
            var targetSymbol = targetCompositionSymbolUi.Symbol;
            var sourceCompositionSymbolUi = SymbolUiRegistry.Entries[_sourceSymbolId];
            
            // copy animations first, so when creating the new child instances can automatically create animations actions for the existing curves
            var childIdsToCopyAnimations = _childrenToCopy.Select(entry => entry.ChildId).ToList();
            var oldToNewIdDict = _childrenToCopy.ToDictionary(entry => entry.ChildId, entry => entry.AddedId);
            sourceCompositionSymbolUi.Symbol.Animator.CopyAnimationsTo(targetSymbol.Animator, childIdsToCopyAnimations, oldToNewIdDict);
            
            foreach (var childToCopy in _childrenToCopy)
            {
                SymbolChild symbolChildToCopy = sourceCompositionSymbolUi.Symbol.Children.Find(child => child.Id == childToCopy.ChildId);
                if (symbolChildToCopy == null)
                {
                    Log.Warning("Skipping attempt to copy undefined operator. This can be related to undo/redo operations. Please try to reproduce and tell pixtur");
                    continue;
                }
                
                var symbolToAdd = SymbolRegistry.Entries[symbolChildToCopy.Symbol.Id];
                targetCompositionSymbolUi.AddChildAsCopyFromSource(symbolToAdd, childToCopy.AddedId, sourceCompositionSymbolUi, childToCopy.ChildId,
                                                                   _targetPosition + childToCopy.RelativePosition);
                
                SymbolChild newSymbolChild = targetSymbol.Children.Find(child => child.Id == childToCopy.AddedId);
                NewSymbolChildIds.Add(newSymbolChild.Id);
                var newSymbolInputs = newSymbolChild.InputValues;
                foreach (var (id, input) in symbolChildToCopy.InputValues)
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
            }

            // add connections between copied children
            foreach (var connection in _connectionsToCopy)
            {
                targetCompositionSymbolUi.Symbol.AddConnection(connection);
            }
        }
        
        public List<Guid> NewSymbolChildIds = new List<Guid>(); //This primarily used for selecting the new children

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

        private readonly Vector2 _targetPosition;
        private readonly Guid _sourceSymbolId;
        private readonly Guid _targetSymbolId;
        private readonly List<Entry> _childrenToCopy = new List<Entry>();
        private readonly List<Symbol.Connection> _connectionsToCopy = new List<Symbol.Connection>();
        public Vector2 PositionOffset;
    }
}