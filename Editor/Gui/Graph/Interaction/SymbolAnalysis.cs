using System;
using System.Collections.Generic;
using System.Linq;

namespace T3.Editor.Gui.Graph.Interaction
{
    /// <summary>
    /// Can aggregate information about all symbols like warnings, dependencies and examples
    /// </summary>
    public static class SymbolAnalysis
    {
        public static void Update()
        {
            InformationForSymbolIds.Clear();
            foreach (var symbolUi in SymbolUiRegistry.Entries.Values)
            {
                InformationForSymbolIds[symbolUi.Symbol.Id]
                    = new SymbolInformation()
                          {
                              RequiredSymbolIds = NodeOperations.CollectRequiredSymbolIds(symbolUi.Symbol),
                              DependingSymbolIds = NodeOperations.GetDependingSymbols(symbolUi.Symbol).Select(s => s.Id).ToHashSet(),
                              ExampleSymbols = SymbolUiRegistry.Entries.Values
                                                               .Where(c => c.Symbol.Name == symbolUi.Symbol.Name + "Example"
                                                                           || c.Symbol.Name == symbolUi.Symbol.Name + "Examples")
                                                               .Select(c => c.Symbol.Id)
                                                               .ToList() 
                          };
            }
        }
        
        public static readonly Dictionary<Guid, SymbolInformation> InformationForSymbolIds = new(1000);

        /// <summary>
        /// 
        /// </summary>
        public class SymbolInformation
        {
            public List<string> Warnings = new();
            public HashSet<Guid> RequiredSymbolIds = new();
            public HashSet<Guid> DependingSymbolIds = new();
            public List<Guid> ExampleSymbols = new();
        }
    }
}