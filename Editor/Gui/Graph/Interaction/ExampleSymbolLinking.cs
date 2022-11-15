using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;

namespace Editor.Gui.Graph.Interaction
{
    public static class ExampleSymbolLinking
    {
        public static void UpdateExampleLinks()
        {
            ExampleSymbols.Clear();
            foreach (var symbolUi in SymbolUiRegistry.Entries.Values)
            {
                var examples = SymbolUiRegistry.Entries.Values
                                               .Where(c => c.Symbol.Name == symbolUi.Symbol.Name + "Example" 
                                                           || c.Symbol.Name == symbolUi.Symbol.Name + "Examples"  )
                                               .Select(c=> c.Symbol.Id)
                                               .ToList();
                if (examples.Count != 0)
                    ExampleSymbols[symbolUi.Symbol.Id] = examples;
            }
            Log.Debug($"Found examples for {ExampleSymbols.Count} operators");
        }
        
        
        public static Dictionary<Guid, List<Guid>> ExampleSymbols { get; }= new Dictionary<Guid, List<Guid>>(30);
    }
}