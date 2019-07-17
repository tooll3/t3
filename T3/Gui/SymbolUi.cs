using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Operator;

namespace T3.Gui
{
    public class SymbolUi
    {
        public Symbol Symbol { get; }

        public SymbolUi(Symbol symbol)
        {
            Symbol = symbol;

            var childrenUis = SymbolChildUiRegistry.Entries[symbol.Id];
            foreach (var child in symbol.Children)
            {
                ChildUis.Add(childrenUis[child.Id]);
            }

            var inputUis = InputUiRegistry.Entries[symbol.Id];
            foreach (var input in symbol.InputDefinitions)
            {
                InputUis.Add(input.Id, inputUis[input.Id]);
            }

            var outputUis = OutputUiRegistry.Entries[symbol.Id];
            foreach (var output in symbol.OutputDefinitions)
            {
                OutputUis.Add(output.Id, outputUis[output.Id]);
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