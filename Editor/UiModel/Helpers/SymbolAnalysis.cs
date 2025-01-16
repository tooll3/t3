#nullable enable
using T3.Core.Operator;
using T3.Editor.UiModel.ProjectSession;

namespace T3.Editor.UiModel.Helpers;

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
        var usages = CollectSymbolUsageCounts();
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
        var usages = CollectSymbolUsageCounts();
            
        InformationForSymbolIds.Clear();

        foreach (var symbolUi in EditorSymbolPackage.AllSymbolUis)
        {
            usages.TryGetValue(symbolUi.Symbol.Id, out var usageCount);
            var requiredSymbols = CollectRequiredSymbols(symbolUi.Symbol);
            var invalidRequirements = CollectInvalidRequirements(symbolUi.Symbol, requiredSymbols).ToList();
            
            InformationForSymbolIds[symbolUi.Symbol.Id]
                = new SymbolInformation
                      {
                          RequiredSymbolIds = requiredSymbols.Select(s => s.Id).ToHashSet(),
                          InvalidRequiredIds = invalidRequirements,
                          DependingSymbols = Structure.CollectDependingSymbols(symbolUi.Symbol).Select(s => s.Id).ToHashSet(),
                          ExampleSymbolsIds =  ExampleSymbolLinking.GetExampleIds(symbolUi.Symbol.Id),
                          UsageCount = usageCount,
                          LacksDescription = string.IsNullOrWhiteSpace(symbolUi.Description),
                          LacksAllParameterDescription = symbolUi.InputUis.Count > 2 && symbolUi.InputUis.Values.All(i => string.IsNullOrWhiteSpace(i.Description)),
                          LacksSomeParameterDescription = symbolUi.InputUis.Count > 2 && symbolUi.InputUis.Values.Any(i => string.IsNullOrWhiteSpace(i.Description)),
                          LacksParameterGrouping = symbolUi.InputUis.Count > 4 && !symbolUi.InputUis.Values.Any(i => i.AddPadding || !string.IsNullOrEmpty(i.GroupTitle)),
                          IsLibOperator = symbolUi.Symbol.Namespace.StartsWith("Lib.") && !symbolUi.Symbol.Name.StartsWith("_") && !symbolUi.Symbol.Namespace.Contains("._"),
                          DependsOnObsoleteOps = requiredSymbols.Select(s => s.GetSymbolUi()).Any(ui => ui.Tags.HasFlag(SymbolUi.SymbolTags.Obsolete)),
                          Tags = symbolUi.Tags,
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
        internal HashSet<Guid> RequiredSymbolIds = [];
        internal HashSet<Guid> DependingSymbols = [];
        public List<Guid> InvalidRequiredIds= [];
        public IReadOnlyList<Guid> ExampleSymbolsIds = [];
        internal int UsageCount;
        internal bool LacksDescription;
        internal bool LacksAllParameterDescription;
        internal bool LacksSomeParameterDescription;
        internal bool LacksParameterGrouping;
        internal bool IsLibOperator;
        internal bool DependsOnObsoleteOps;
        internal SymbolUi.SymbolTags Tags;  // Copy to avoid reference to symbolUi
    }

    private static HashSet<Symbol> CollectRequiredSymbols(Symbol root)
    {
        var all = new HashSet<Symbol>();
        Collect(root);
        return all;

        void Collect(Symbol symbol)
        {
            foreach (var symbolChild in symbol.Children.Values)
            {
                if (!all.Add(symbolChild.Symbol))
                    continue;

                Collect(symbolChild.Symbol);
            }
        }
    }
    
    /// <summary>
    /// Collect Ids to required symbols that are not within the list of projects
    /// </summary>
    private static IEnumerable<Guid> CollectInvalidRequirements(Symbol root, HashSet<Symbol> requiredSymbols)
    {
        var result = new List<Symbol>();
        
        // Todo: implement this correctly 
        HashSet<string> validPackagesNames = [
            "Types",
            "Lib", 
            root.Namespace.Split('.')[0]];

        foreach (var r in requiredSymbols)
        {
            var projectId = r.Namespace.Split('.')[0];

            if (validPackagesNames.Contains(projectId))
                continue;

            result.Add(r);
        }

        return result.OrderBy(s => s.Namespace).ThenBy(s => s.Name).Select(s => s.Id);
    }

    private static Dictionary<Guid, int> CollectSymbolUsageCounts()
    {
        var results = new Dictionary<Guid, int>();

        foreach (var s in EditorSymbolPackage.AllSymbols)
        {
            foreach (var child in s.Children.Values)
            {
                results.TryGetValue(child.Symbol.Id, out var currentCount);
                results[child.Symbol.Id] = currentCount + 1;
            }
        }

        return results;
    }
}