using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.External.Truncon.Collections;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.OutputUi;
using T3.Editor.Gui.Selection;

namespace T3.Editor.UiModel
{
    public sealed partial class SymbolUi : ISelectionContainer
    {
        internal Symbol Symbol => _package.Symbols[_id];
        private readonly SymbolPackage _package;
        private readonly Guid _id;

        internal SymbolUi(Symbol symbol, bool updateConsistency)
        {
            _id = symbol.Id;
            _package = symbol.SymbolPackage;
            if(_package == null)
                throw new ArgumentException("Symbol must have a package");
            
            InputUis = new Dictionary<Guid, IInputUi>();
            OutputUis = new Dictionary<Guid, IOutputUi>();
            Annotations = new OrderedDictionary<Guid, Annotation>();
            Links = new OrderedDictionary<Guid, ExternalLink>();
            
            if (updateConsistency)
                UpdateConsistencyWithSymbol();

            ReadOnly = true;
        }

        internal SymbolUi(Symbol symbol,
                        Func<Symbol, List<Child>> childUis,
                        IDictionary<Guid, IInputUi> inputs,
                        IDictionary<Guid, IOutputUi> outputs,
                        IDictionary<Guid, Annotation> annotations,
                        OrderedDictionary<Guid, ExternalLink> links,
                        bool updateConsistency) : this(symbol, false)
        {
            _childUis = childUis(symbol).ToDictionary(x => x.Id, x => x);
            
            InputUis = inputs;
            OutputUis = outputs;
            Annotations = annotations;
            Links = links;
            ReadOnly = true;
            
            if (updateConsistency)
                UpdateConsistencyWithSymbol(symbol);
        }

        IEnumerable<ISelectableCanvasObject> ISelectionContainer.GetSelectables() => GetSelectables();

        internal IEnumerable<ISelectableCanvasObject> GetSelectables()
        {
            foreach (var childUi in ChildUis.Values)
                yield return childUi;

            foreach (var inputUi in InputUis)
                yield return inputUi.Value;

            foreach (var outputUi in OutputUis)
                yield return outputUi.Value;

            foreach (var annotation in Annotations)
                yield return annotation.Value;
        }

        /// <summary>
        /// Updates the consistency of the symbol ui with the symbol.
        /// Requires providing a symbol as an argument if the symbol is not part of a package - i.e. if it is a pasted symbol
        /// </summary>
        /// <param name="symbol"></param>
        internal void UpdateConsistencyWithSymbol(Symbol symbol = null)
        {
            symbol ??= Symbol;
            // Check if child entries are missing
            foreach (var child in symbol.Children.Values)
            {
                var childId = child.Id;
                if (!ChildUis.TryGetValue(childId, out _))
                {
                    Log.Debug($"Found no symbol child ui entry for symbol child '{child.ReadableName}' - creating a new one");
                    var childUi = new Child(childId, _id, (EditorSymbolPackage)symbol.SymbolPackage)
                                      {
                                          PosOnCanvas = new Vector2(100, 100),
                                      };
                    _childUis.Add(childId, childUi);
                }
            }

            // check if there are child entries where no symbol child exists anymore
            List<Guid> childIdsToRemove = new(4);

            foreach (var childUi in _childUis.Values)
            {
                if(!symbol.Children.ContainsKey(childUi.Id))
                    childIdsToRemove.Add(childUi.Id);
            }
            
            foreach (var id in childIdsToRemove)
            {
                _childUis.Remove(id);
            }

            // check if input UIs are missing
            var existingInputs = InputUis.Values.ToList();
            InputUis.Clear();
            for (int i = 0; i < symbol.InputDefinitions.Count; i++)
            {
                Symbol.InputDefinition input = symbol.InputDefinitions[i];
                var existingInputUi = existingInputs.SingleOrDefault(inputUi => inputUi.Id == input.Id);
                if (existingInputUi == null || existingInputUi.Type != input.DefaultValue.ValueType)
                {
                    Log.Debug($"Found no input ui entry for symbol child input '{symbol.Name}.{input.Name}' - creating a new one");
                    InputUis.Remove(input.Id);
                    var newInputUi = InputUiFactory.Instance.CreateFor(input.DefaultValue.ValueType);
                    newInputUi.Parent = this;
                    newInputUi.InputDefinition = input;
                    newInputUi.PosOnCanvas = GetCanvasPositionForNextInputUi(this);
                    InputUis.Add(input.Id, newInputUi);
                }
                else
                {
                    existingInputUi.Parent = this;
                    InputUis.Add(existingInputUi.Id, existingInputUi); // add at correct position
                }
            }

            // check if there are input entries where no input ui exists anymore
            foreach (var inputUiToRemove in InputUis.Where(kv => !symbol.InputDefinitions.Exists(inputDef => inputDef.Id == kv.Key)).ToList())
            {
                Log.Debug($"InputUi '{inputUiToRemove.Value.Id}' still existed but no corresponding input definition anymore. Removing the ui.");
                InputUis.Remove(inputUiToRemove.Key);
            }

            foreach (var output in symbol.OutputDefinitions)
            {
                if (!OutputUis.TryGetValue(output.Id, out var value) || (value.Type != output.ValueType))
                {
                    Log.Debug($"Found no output ui for '{symbol.Name}.{output.Name}' - creating a new one");
                    OutputUis.Remove(output.Id); // if type has changed remove the old entry

                    var newOutputUi = OutputUiFactory.Instance.CreateFor(output.ValueType);
                    newOutputUi.OutputDefinition = output;
                    newOutputUi.PosOnCanvas = ComputeNewOutputUiPositionOnCanvas(_childUis.Values, OutputUis.Values);
                    OutputUis.Add(output.Id, newOutputUi);
                    FlagAsModified();
                }
            }

            // check if there are input entries where no output ui exists anymore
            foreach (var outputUiToRemove in OutputUis.Where(kv => !Symbol.OutputDefinitions.Exists(outputDef => outputDef.Id == kv.Key)).ToList())
            {
                Log.Debug($"OutputUi '{outputUiToRemove.Value.Id}' still existed but no corresponding input definition anymore. Removing the ui.");
                OutputUis.Remove(outputUiToRemove.Key);
            }
        }

        private static Vector2 ComputeNewOutputUiPositionOnCanvas(IEnumerable<Child> childUis, IEnumerable<IOutputUi> outputUis)
        {
            bool setByOutputs = false;
            var maxPos = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var output in outputUis)
            {
                maxPos = Vector2.Max(maxPos, output.PosOnCanvas);
                setByOutputs = true;
            }

            if (setByOutputs)
                return maxPos + new Vector2(0, 100);

            // FIXME: childUis are always undefined at this point?
            var setByChildren = false;
            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;

            var maxX = float.NegativeInfinity;

            foreach (var childUi in childUis)
            {
                minY = MathUtils.Min(childUi.PosOnCanvas.Y, minY);
                maxY = MathUtils.Max(childUi.PosOnCanvas.Y, maxY);

                maxX = MathUtils.Max(childUi.PosOnCanvas.X, maxX);
                setByChildren = true;
            }

            if (setByChildren)
                return new Vector2(maxX + 100, (maxY + minY) / 2);

            //Log.Warning("Assuming default output position");
            return new Vector2(300, 200);
        }

        private Vector2 GetCanvasPositionForNextInputUi(SymbolUi symbolUi)
        {
            if (symbolUi.Symbol.InputDefinitions.Count == 0)
            {
                return new Vector2(-200, 0);
            }

            IInputUi lastInputUi = null;

            foreach (var inputDef in symbolUi.Symbol.InputDefinitions)
            {
                if (symbolUi.InputUis.TryGetValue(inputDef.Id, out var ui))
                    lastInputUi = ui;
            }

            if (lastInputUi == null)
                return new Vector2(-200, 0);

            return lastInputUi.PosOnCanvas + new Vector2(0, lastInputUi.Size.Y + SelectableNodeMovement.SnapPadding.Y);
        }

        internal void ClearModifiedFlag()
        {
            _hasBeenModified = false;
        }

        internal string Description { get; set; } = string.Empty;

        internal bool ReadOnly;
        private bool _hasBeenModified;
        internal bool HasBeenModified => _hasBeenModified;
        internal bool NeedsSaving => _hasBeenModified && !ReadOnly;
        private  Dictionary<Guid, Child> _childUis = new();
        internal IReadOnlyDictionary<Guid, Child> ChildUis => _childUis;
        internal IDictionary<Guid, ExternalLink> Links { get; private set; }
        internal IDictionary<Guid, IInputUi> InputUis { get; private set; } 
        internal IDictionary<Guid, IOutputUi> OutputUis{ get; private set; }
        internal IDictionary<Guid, Annotation> Annotations { get; private set; }

        internal void ReplaceWith(SymbolUi newSymbolUi)
        {
            _childUis = newSymbolUi._childUis;
            InputUis = newSymbolUi.InputUis;
            OutputUis = newSymbolUi.OutputUis;
            Annotations = newSymbolUi.Annotations;
            Links = newSymbolUi.Links;
            Description = newSymbolUi.Description;
        }
    }
}