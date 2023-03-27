using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Editor.Gui.Graph.Interaction
{
    public static class ExampleSymbolLinking
    {
        public static void UpdateExampleLinks()
        {
            ExampleSymbols.Clear();

            var symbolCollection = SymbolUiRegistry.Entries.Values;
            var exampleText = "Example";
            var examplesText = "Examples";

            var potentialExamples = new List<Symbol>();

            foreach (var s in symbolCollection)
            {
                var name = s.Symbol.Name;
                var potentialExample = name.EndsWith(exampleText) || name.EndsWith(examplesText);
                if(potentialExample)
                    potentialExamples.Add(s.Symbol);
            }

            Log.Debug($"Found {potentialExamples.Count.ToString()} potential examples");
            
            foreach (var symbolUi in symbolCollection)
            {
                var symbol = symbolUi.Symbol;
                var exampleName = symbol.Name + exampleText;
                var examplesName = symbol.Name + examplesText;
                
                var examples = potentialExamples
                              .Where(c => c.Name == exampleName || c.Name == examplesName)
                              .Select(c => c.Id)
                              .ToList();
                
                if (examples.Count != 0)
                    ExampleSymbols[symbol.Id] = examples;

            }

            Log.Debug($"Found examples for {ExampleSymbols.Count} operators");
        }

        public static Dictionary<Guid, List<Guid>> ExampleSymbols { get; } = new Dictionary<Guid, List<Guid>>(30);
    }
}