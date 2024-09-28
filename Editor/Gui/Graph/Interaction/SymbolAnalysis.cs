using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction;

/// <summary>
/// Can aggregate information about all symbols like warnings, dependencies and examples
/// </summary>
public static class SymbolAnalysis
{
    /// <summary>
    /// Basic information is used by symbol browser and for relevancy search 
    /// </summary>
    public static void UpdateUsagesOnly()
    {
        var usages = Structure.CollectSymbolUsageCounts();
        ConnectionHashCounts = new Dictionary<int, int>();
            
        foreach (var symbolUi in EditorSymbolPackage.AllSymbolUis)
        {
            var symbolId = symbolUi.Symbol.Id;
            if (!InformationForSymbolIds.TryGetValue(symbolId, out var info))
            {
                info = new SymbolInformation();
                    
                InformationForSymbolIds[symbolId] = info;
            }
                
            // Update connection counts
            foreach (var connection in symbolUi.Symbol.Connections)
            {
                var hash = connection.SourceSlotId.GetHashCode() * 31 + connection.TargetSlotId.GetHashCode();
                ConnectionHashCounts.TryGetValue(hash, out var connectionCount);
                ConnectionHashCounts[hash] = connectionCount + 1;
            }
                
            usages.TryGetValue(symbolId, out var count);
            info.UsageCount = count;
        }            
    }
        
        
        
    public static void UpdateDetails()
    {
        var usages = Structure.CollectSymbolUsageCounts();
            
        InformationForSymbolIds.Clear();

        var allSymbolUis = EditorSymbolPackage.AllSymbolUis.ToArray();
        foreach (var symbolUi in allSymbolUis)
        {
            usages.TryGetValue(symbolUi.Symbol.Id, out var usageCount);
                
                

            InformationForSymbolIds[symbolUi.Symbol.Id]
                = new SymbolInformation()
                      {
                          RequiredSymbols = Structure.CollectRequiredSymbols(symbolUi.Symbol),
                          DependingSymbols = Structure.CollectDependingSymbols(symbolUi.Symbol).ToHashSet(),
                          ExampleSymbols = allSymbolUis
                                          .Where(c => c.Symbol.Name == symbolUi.Symbol.Name + "Example"
                                                      || c.Symbol.Name == symbolUi.Symbol.Name + "Examples")
                                          .Select(c => c.Symbol.Id)
                                          .ToList(),
                          UsageCount = usageCount,
                          LacksDescription = string.IsNullOrWhiteSpace(symbolUi.Description),
                          LacksAllParameterDescription = symbolUi.InputUis.Count > 2 && symbolUi.InputUis.Values.All(i => string.IsNullOrWhiteSpace(i.Description)),
                          LacksSomeParameterDescription = symbolUi.InputUis.Count > 2 && symbolUi.InputUis.Values.Any(i => string.IsNullOrWhiteSpace(i.Description)),
                          LacksParameterGrouping = symbolUi.InputUis.Count > 4 && !symbolUi.InputUis.Values.Any(i => i.AddPadding || !string.IsNullOrEmpty(i.GroupTitle)),
                          IsLibOperator = symbolUi.Symbol.Namespace.StartsWith("Lib.") && !symbolUi.Symbol.Name.StartsWith("_") && !symbolUi.Symbol.Namespace.Contains("._"),
                      };
                
        }

        DetailsInitialized = true;
    }
        
    public static readonly Dictionary<Guid, SymbolInformation> InformationForSymbolIds = new(1000);
        
    /// <summary>
    /// We are storing all connections between input slots as hashes from _sourceSlotId x _targetSlotId.
    /// These are then used by SymbolBrowser to increase relevancy for frequent combinations.
    /// </summary>
    public static Dictionary<int, int> ConnectionHashCounts = new();
    public static bool DetailsInitialized { get; private set; } 

    /// <summary>
    /// 
    /// </summary>
    public class SymbolInformation
    {
        public List<string> Warnings = new();
        public HashSet<Symbol> RequiredSymbols = new();
        public HashSet<Symbol> DependingSymbols = new();
        public List<Guid> ExampleSymbols = new();
        public int UsageCount { get; set; }
        public bool LacksDescription;
        public bool LacksAllParameterDescription;
        public bool LacksSomeParameterDescription;
        public bool LacksParameterGrouping;
        public bool IsLibOperator;
            
    }
}