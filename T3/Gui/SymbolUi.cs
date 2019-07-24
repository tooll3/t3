using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui
{
    public class SymbolUi
    {
        public Symbol Symbol { get; }

        public SymbolUi(Symbol symbol)
        {
            Symbol = symbol;
            UpdateConsistencyWithSymbol(); // this sets up all missing elements
        }

        public SymbolUi(Symbol symbol, List<SymbolChildUi> childUis, Dictionary<Guid, IInputUi> inputs, Dictionary<Guid, IOutputUi> outputs)
        {
            Symbol = symbol;
            ChildUis = childUis;
            InputUis = inputs;
            OutputUis = outputs;

            UpdateConsistencyWithSymbol();
        }

        public void UpdateConsistencyWithSymbol()
        {
            // check if child entries are missing
            foreach (var child in Symbol.Children)
            {
                if (!ChildUis.Exists(c => c.Id == child.Id))
                {
                    Log.Debug($"Found no symbol child ui entry for symbol child '{child.ReadableName}' - creating a new one");
                    var childUi = new SymbolChildUi()
                                  {
                                      SymbolChild = child,
                                      PosOnCanvas = new Vector2(100, 100)
                                  };
                    ChildUis.Add(childUi);
                }
            }

            // check if there are child entries where no symbol child exists anymore
            ChildUis.RemoveAll(childUi => !Symbol.Children.Exists(child => child.Id == childUi.Id));

            // check if input uis are missing
            var inputUiFactory = InputUiFactory.Entries;
            foreach (var input in Symbol.InputDefinitions)
            {
                if (!InputUis.TryGetValue(input.Id, out var value) || value.Type != input.DefaultValue.ValueType)
                {
                    Log.Debug($"Found no input ui entry for symbol child input '{input.Name}' - creating a new one");
                    InputUis.Remove(input.Id);
                    var inputCreator = inputUiFactory[input.DefaultValue.ValueType];
                    InputUis.Add(input.Id, inputCreator(input.Id));
                }
            }

            var outputUiFactory = OutputUiFactory.Entries;
            foreach (var output in Symbol.OutputDefinitions)
            {
                if (!OutputUis.TryGetValue(output.Id, out var value) || (value.Type != output.ValueType))
                {
                    Log.Debug($"Found no output ui entry for symbol child output '{output.Name}' - creating a new one");
                    OutputUis.Remove(output.Id);
                    var outputUiCreator = outputUiFactory[output.ValueType];
                    OutputUis.Add(output.Id, outputUiCreator(output.Id));
                }
            }
        }
        public Guid AddChild(Symbol symbolToAdd, Vector2 posInCanvas, Vector2 size, bool isVisible)
        {
            Guid addedChildId = Symbol.AddChild(symbolToAdd);
            var childUi = new SymbolChildUi
                          {
                              SymbolChild = Symbol.Children.Find(entry => entry.Id == addedChildId),
                              PosOnCanvas = posInCanvas,
                              Size = size,
                              IsVisible = isVisible
                          };
            ChildUis.Add(childUi);

            return addedChildId;
        }

        public void RemoveChild(Guid id)
        {
            Symbol.RemoveChild(id); // remove from symbol

            // now remove ui entry
            var childToRemove = ChildUis.Single(child => child.Id == id);
            ChildUis.Remove(childToRemove);
        }

        public List<SymbolChildUi> ChildUis = new List<SymbolChildUi>();
        public Dictionary<Guid, IInputUi> InputUis = new Dictionary<Guid, IInputUi>();
        public Dictionary<Guid, IOutputUi> OutputUis = new Dictionary<Guid, IOutputUi>();
    }

    public static class SymbolUiRegistry
    {
        public static Dictionary<Guid, SymbolUi> Entries { get; } = new Dictionary<Guid, SymbolUi>(20);
    }
}