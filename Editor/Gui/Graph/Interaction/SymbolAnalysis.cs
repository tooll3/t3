#nullable enable
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction;

/// <summary>
/// Can aggregate information about all symbols like warnings, dependencies and examples
/// </summary>
internal static class SymbolAnalysis
{
    /// <summary>
    /// Basic information is used by symbol browser and for relevancy search 
    /// </summary>
    internal static void UpdateSymbolUsageCounts()
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

    /// <summary>
    /// Update <see cref="InformationForSymbolIds"/> collection with details that might be useful for tasks like
    /// library structure cleanup. 
    /// </summary>
    internal static void UpdateDetails()
    {
        var usages = Structure.CollectSymbolUsageCounts();
            
        InformationForSymbolIds.Clear();

        foreach (var symbolUi in EditorSymbolPackage.AllSymbolUis)
        {
            usages.TryGetValue(symbolUi.Symbol.Id, out var usageCount);
            var examples = 
            
            InformationForSymbolIds[symbolUi.Symbol.Id]
                = new SymbolInformation
                      {
                          RequiredSymbols = CollectRequiredSymbols(symbolUi.Symbol),
                          DependingSymbols = Structure.CollectDependingSymbols(symbolUi.Symbol).ToHashSet(),
                          ExampleSymbolsIds =  ExampleSymbolLinking.GetExampleIds(symbolUi.Symbol.Id),
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

    internal static readonly Dictionary<Guid, SymbolInformation> InformationForSymbolIds = new(1000);
        
    /// <summary>
    /// We are storing all connections between input slots as hashes from _sourceSlotId x _targetSlotId.
    /// These are then used by SymbolBrowser to increase relevancy for frequent combinations.
    /// </summary>
    internal static Dictionary<int, int> ConnectionHashCounts = new();

    internal static bool DetailsInitialized { get; private set; } 

    /// <summary>
    /// 
    /// </summary>
    public sealed class SymbolInformation
    {
        public List<string> Warnings = [];
        internal HashSet<Symbol> RequiredSymbols = [];
        internal HashSet<Symbol> DependingSymbols = [];
        public IReadOnlyList<Guid> ExampleSymbolsIds = [];
        internal int UsageCount { get; set; }
        internal bool LacksDescription;
        internal bool LacksAllParameterDescription;
        internal bool LacksSomeParameterDescription;
        internal bool LacksParameterGrouping;
        internal bool IsLibOperator;
            
    }

    public static HashSet<Symbol> CollectRequiredSymbols(Symbol symbol, HashSet<Symbol>? all = null)
    {
        all ??= [];

        foreach (var symbolChild in symbol.Children.Values)
        {
            if (!all.Add(symbolChild.Symbol))
                continue;

            CollectRequiredSymbols(symbolChild.Symbol, all);
        }

        return all;
    }
}