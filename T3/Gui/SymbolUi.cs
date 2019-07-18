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

        public SymbolUi(Symbol symbol, List<SymbolChildUi> childUis, Dictionary<Guid, IInputUi> inputs, Dictionary<Guid, IOutputUi> outputs)
        {
            Symbol = symbol;
            ChildUis = childUis;
            InputUis = inputs;
            OutputUis = outputs;

            CheckConsistency();
        }

        private void CheckConsistency()
        {
            // todo: adjust the code below for SymbolUi
            // var childUiEntries = SymbolChildUiRegistry.Entries[symbol.Id];
            // foreach (var child in symbol.Children)
            // {
            //     if (!childUiEntries.ContainsKey(child.Id))
            //     {
            //         Log.Info($"Found no symbol child ui dictionary entry for symbol child '{child.ReadableName}' - creating a new one");
            //         var childUi = new SymbolChildUi()
            //         {
            //             SymbolChild = child,
            //             PosOnCanvas = new Vector2(100, 100)
            //         };
            //         childUiEntries.Add(child.Id, childUi);
            //     }
            // }
            //
            // if (!InputUiRegistry.Entries.TryGetValue(symbol.Id, out var inputDict))
            // {
            //     Log.Info($"Found no input ui dictionary entry for symbol '{symbol.Name}' - creating a new one");
            //     inputDict = new Dictionary<Guid, IInputUi>();
            //     InputUiRegistry.Entries.Add(symbol.Id, inputDict);
            // }
            //
            // var inputUiFactory = InputUiFactory.Entries;
            // foreach (var input in symbol.InputDefinitions)
            // {
            //     if (!inputDict.TryGetValue(input.Id, out var value) || (value.Type != input.DefaultValue.ValueType))
            //     {
            //         inputDict.Remove(input.Id);
            //         var inputCreator = inputUiFactory[input.DefaultValue.ValueType];
            //         inputDict.Add(input.Id, inputCreator());
            //     }
            // }
            //
            // if (!OutputUiRegistry.Entries.TryGetValue(symbol.Id, out var outputDict))
            // {
            //     Log.Info($"Found no output ui dictionary entry for symbol '{symbol.Name}' - creating a new one.");
            //     outputDict = new Dictionary<Guid, IOutputUi>();
            //     OutputUiRegistry.Entries.Add(symbol.Id, outputDict);
            // }

            // var symbolUi = new SymbolUi(symbol);
            // SymbolUiRegistry.Entries.Add(symbol.Id, symbolUi);

            // var outputUiFactory = OutputUiFactory.Entries;
            // foreach (var output in symbol.OutputDefinitions)
            // {
            //     if (!outputDict.TryGetValue(output.Id, out var value) || (value.Type != output.ValueType))
            //     {
            //         outputDict.Remove(output.Id);
            //         var outputUiCreator = outputUiFactory[output.ValueType];
            //         outputDict.Add(output.Id, outputUiCreator());
            //     }
            // }
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