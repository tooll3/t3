#nullable enable
using System.Text.RegularExpressions;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction;

/// <summary>
/// Provides a regular expression to filter and sort matching <see cref="Symbol"/>s
/// </summary>
internal sealed class SymbolFilter
{
    public string SearchString = string.Empty;  // not a property to allow ref passing
    public Type? FilterInputType {
        get => _inputType;
        set
        {
            _needsUpdate = true;
            _inputType = value;
        }
    }
    public Type? FilterOutputType {
        get => _outputType;
        set
        {
            _needsUpdate = true;
            _outputType = value;
        }
    }

    public bool OnlyMultiInputs { get; set; }
    public List<SymbolUi> MatchingSymbolUis { get; private set; } = [];

    public void UpdateIfNecessary(NodeSelection? selection, bool forceUpdate = false, int limit=30)
    {
        _needsUpdate |= forceUpdate;
        _needsUpdate |= UpdateFilters(SearchString, 
                                      ref _lastSearchString, 
                                      ref _symbolFilterString, 
                                      ref PresetFilterString, 
                                      ref _currentRegex);
            
        if (_needsUpdate)
        {
            //UpdateConnectSlotHashes();  //TODO: Clarify why this is commented out
            UpdateMatchingSymbols(selection, limit);
        }

        WasUpdated = _needsUpdate;
        _needsUpdate = false;
    }

    private static bool UpdateFilters(string search, 
                                      ref string lastSearch, ref string symbolFilter, ref string presetFilter, ref Regex searchRegex)
    {
        if (search == lastSearch)
            return false;
            
        lastSearch = search;
            
        // Check if template search was initiated 
        var twoPartSearchResult = new Regex(@"(.+?)\s+(.*)").Match(search);
        if (twoPartSearchResult.Success)
        {
            symbolFilter = twoPartSearchResult.Groups[1].Value;
            presetFilter = twoPartSearchResult.Groups[2].Value;
        }
        else
        {
            symbolFilter = search;
            presetFilter = string.Empty;
        }
            
        var pattern = string.Join(".*", symbolFilter.ToCharArray());
        try
        {
            searchRegex = new Regex(pattern, RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            Log.Debug("Invalid Regex format: " + pattern);
            return true;
        }

        return true;
    }        
        
    /// <summary>
    /// Build hashes for symbol specific input slots. These are then used
    /// the compute relevancy. 
    /// </summary>
    private void UpdateConnectSlotHashes(GraphWindow window)
    {
        _sourceInputHash = 0;
        _targetInputHash = 0;
    
        var tempConnections = ConnectionMaker.GetTempConnectionsFor(window);
    
        foreach (var c in tempConnections)
        {
            switch (c.GetStatus())
            {
                case ConnectionMaker.TempConnection.Status.SourceIsDraftNode:
                    _targetInputHash = c.TargetSlotId.GetHashCode();
                    break;
    
                case ConnectionMaker.TempConnection.Status.TargetIsDraftNode:
                    _sourceInputHash = c.SourceSlotId.GetHashCode();
                    break;
            }
        }
    }

        
    private void UpdateMatchingSymbols(NodeSelection? selection, int limit)
    {
        var compositionInstance = selection?.GetSelectedComposition();
        ICollection<Guid> parentSymbolIds = compositionInstance != null
                                                ? new HashSet<Guid>(Structure.CollectParentInstances(compositionInstance).Append(compositionInstance).Select(p => p.Symbol.Id))
                                                : Array.Empty<Guid>();

        MatchingSymbolUis.Clear();
        foreach (var symbolUi in EditorSymbolPackage.AllSymbolUis)
        {
            var symbolUiSymbol = symbolUi.Symbol;
                
            if (symbolUiSymbol == null)
            {
                Log.Warning($"Skipping SymbolUi definition with inconsistent symbol...");
                continue;
            }
                
            // Prevent graph cycles
            if (parentSymbolIds.Contains(symbolUiSymbol.Id))
                continue;

            if (_inputType != null)
            {
                var matchingInputDef = symbolUiSymbol.GetInputMatchingType(FilterInputType);
                if (matchingInputDef == null)
                    continue;

                if (OnlyMultiInputs && !matchingInputDef.IsMultiInput)
                    continue;
            }

            if (_outputType != null)
            {
                var matchingOutputDef = symbolUiSymbol.GetOutputMatchingType(FilterOutputType);
                if (matchingOutputDef == null)
                    continue;
            }

            if (!(_currentRegex.IsMatch(symbolUiSymbol.Name)
                  || symbolUiSymbol.Namespace.Contains(_symbolFilterString, StringComparison.InvariantCultureIgnoreCase)
                  || (!string.IsNullOrEmpty(symbolUi.Description)
                      && symbolUi.Description.Contains(_symbolFilterString, StringComparison.InvariantCultureIgnoreCase))))
                continue;

            MatchingSymbolUis.Add(symbolUi);
        }

        EditorSymbolPackage? currentProject = null;
        Instance? composition = null;

        if (GraphWindow.Focused != null)
        {
            currentProject = GraphWindow.Focused.Package;
            composition = GraphWindow.Focused.CompositionOp;
        }
        
        MatchingSymbolUis = MatchingSymbolUis.OrderBy(s => ComputeRelevancy(s, _symbolFilterString, currentProject, composition))
                                             .Reverse()
                                             .Take(limit)
                                             .ToList();
    }

    private double ComputeRelevancy(SymbolUi symbolUi, string query, EditorSymbolPackage? currentProject, Instance? composition)
    {
        float relevancy = 1;

        var symbol = symbolUi.Symbol;
        var symbolName = symbol.Name;

        if (symbolName.Equals(query, StringComparison.InvariantCultureIgnoreCase))
        {
            relevancy *= 5;
        }

        if (symbolName.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
        {
            // bump if starts
            relevancy *= 4.5f;
        }
        else
        {
            // bump if direct match
            if (symbolName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                relevancy *= 3f;
            }
        }

        if (!string.IsNullOrEmpty(symbolUi.Description)
            && symbolUi.Description.Contains(query, StringComparison.InvariantCultureIgnoreCase))
        {
            relevancy *= 1.01f;
        }

        if (symbolName.Equals(query, StringComparison.InvariantCultureIgnoreCase))
        {
            relevancy *= 5;
        }

        // Add usage count (the following statement is slow and should be cached)
        var count = SymbolAnalysis.InformationForSymbolIds.TryGetValue(symbol.Id, out var info)
                        ? info.UsageCount
                        : 0;

        //symbolUi.Symbol.InstancesOfSymbol.Select(instance =>instance.SymbolChildId).Distinct().Count();
        relevancy *= 1 + count / 100f;

        // Bump if characters match upper characters
        // e.g. "ds" matches "DrawState"
        var pascalCaseMatch = true;
        var maxIndex = 0;
        var uppercaseQuery = query.ToUpper();
        for (var charIndex = 0; charIndex < uppercaseQuery.Length; charIndex++)
        {
            var c = uppercaseQuery[charIndex];
            var indexInName = symbolName.IndexOf(c);
            if (indexInName < maxIndex)
            {
                pascalCaseMatch = false;
                break;
            }

            maxIndex = indexInName;
        }

        if (pascalCaseMatch)
        {
            relevancy *= 5f;
        }

        if (!string.IsNullOrEmpty(symbol.Namespace))
        {
            if (symbol.Namespace.Contains("dx11")
                || symbol.Namespace.Contains("_"))
                relevancy *= 0.1f;

            if (symbol.Namespace.StartsWith("Lib"))
            {
                relevancy *= 3f;
            }

            if (symbol.Namespace.StartsWith("examples"))
            {
                relevancy *= 2f;
            }
        }

        if (symbolName.StartsWith("_"))
        {
            relevancy *= 0.1f;
        }

        if (symbolName.Contains("OBSOLETE"))
        {
            relevancy *= 0.01f;
        }

        var symbolId = symbol.Id;
        var symbolPackage = symbol.SymbolPackage;
        if (currentProject != null)
        {
            // mega-boost symbols from the same package as the current project
            if (currentProject == symbolPackage)
            {
                relevancy *= 5f;
            }
            
            // or boost symbols from related namespaces
            else if (symbol.Namespace!.StartsWith(currentProject.RootNamespace))
            {
                relevancy *= 1.9f;
            }
        }

        if (composition != null)
        {
            var compositionSymbol = composition.Symbol;
            var compositionPackage = compositionSymbol.SymbolPackage;
            
            // boost symbols from the same package as composition, or from related namespaces
            if (compositionPackage.Symbols.ContainsKey(symbolId) || symbolPackage.RootNamespace.StartsWith(compositionPackage.RootNamespace))
            {
                relevancy *= 1.9f;
            }
        }

        // boost user symbols
        if (symbolPackage is EditableSymbolProject)
        {
            relevancy *= 1.9f;
        }

        // Bump operators with matching connections 
        var matchingConnectionsCount = 0;
        if (_sourceInputHash != 0)
        {
            foreach (var inputDefinition in symbol.InputDefinitions.FindAll(i => i.DefaultValue.ValueType == FilterInputType))
            {
                var connectionHash = _sourceInputHash * 31 + inputDefinition.Id.GetHashCode();

                if (SymbolAnalysis.ConnectionHashCounts.TryGetValue(connectionHash, out var connectionCount))
                {
                    //Log.Debug($" <{connectionCount}x> --> {symbol.Name}");
                    matchingConnectionsCount += connectionCount;
                }
            }
        }
            
        if (_targetInputHash != 0)
        {
            foreach (var outputDefinition in symbol.OutputDefinitions.FindAll(o => o.ValueType == FilterOutputType))
            {
                var connectionHash = outputDefinition.Id.GetHashCode() * 31 + _targetInputHash;

                if (SymbolAnalysis.ConnectionHashCounts.TryGetValue(connectionHash, out var connectionCount))
                {
                    //Log.Debug($"  {symbol.Name} --> <{connectionCount}x>");
                    matchingConnectionsCount += connectionCount;
                }
            }
        }

        if (matchingConnectionsCount > 0)
        {
            relevancy *= 1 + MathF.Pow(matchingConnectionsCount, 0.33f) * 4f;
            //Log.Debug($"Bump relevancy {symbol.Name}  {oldRelevancy:0.00} -> {relevancy:0.00}");
        }

        return relevancy;
    }

        
    private bool _needsUpdate;
    private string _symbolFilterString = string.Empty;
    public string PresetFilterString = string.Empty;

    private Type? _inputType;
    private Type? _outputType;
    public bool WasUpdated;

    private static int _sourceInputHash;
    private int _targetInputHash;

    private Regex _currentRegex = new(".*", RegexOptions.IgnoreCase);
    private string _lastSearchString =string.Empty;
}
