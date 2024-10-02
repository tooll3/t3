using System;
using System.Collections.Generic;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction
{
    public static class ExampleSymbolLinking
    {
        public static void UpdateExampleLinks()
        {
            ExampleSymbols.Clear();

            const string exampleSuffix = "Example";
            const string examplesSuffix = "Examples";

            var potentialExamples = new List<(string correspondingSymbolName, Symbol exampleSymbol)>();

            var symbolUiCollection = SymbolUiRegistry.Entries.Values;
            Dictionary<string, Symbol> symbolsByName = new(capacity: symbolUiCollection.Count);
            foreach (var symbolUi in symbolUiCollection)
            {
                var symbol = symbolUi.Symbol;
                var name = symbol.Name;
                
                symbolsByName.Add(name, symbol);
                
                var endsWithExample = name.EndsWith(exampleSuffix);

                if (endsWithExample)
                {
                    var correspondingSymbolName = name.Substring(0, name.Length - exampleSuffix.Length);
                    potentialExamples.Add((correspondingSymbolName, symbol));
                    continue;
                }
                
                var endsWithExamples = name.EndsWith(examplesSuffix);

                if (endsWithExamples)
                {
                    var correspondingSymbolName = name.Substring(0, name.Length - examplesSuffix.Length);
                    potentialExamples.Add((correspondingSymbolName, symbol));
                }
            }

            Log.Debug($"Found {potentialExamples.Count.ToString()} potential examples");
            
            foreach (var potentialExample in potentialExamples)
            {
                var gotSymbol = symbolsByName.TryGetValue(potentialExample.correspondingSymbolName, out var symbol);
                
                if (!gotSymbol)
                    continue;

                var hasExampleList = ExampleSymbols.TryGetValue(symbol.Id, out var exampleList);

                if (!hasExampleList)
                {
                    exampleList = new();
                    ExampleSymbols.Add(symbol.Id, exampleList);
                }
                
                exampleList.Add(potentialExample.exampleSymbol.Id);
            }

            Log.Debug($"Found examples for {ExampleSymbols.Count} operators");
        }

        public static Dictionary<Guid, List<Guid>> ExampleSymbols { get; } = new(50);
    }
}