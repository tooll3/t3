using System.Text.RegularExpressions;
using T3.Core.Operator;
using T3.Editor.Gui.Graph.Helpers;
using T3.Editor.Gui.Graph.Interaction.Connections;
using T3.Editor.Gui.Interaction.Variations.Model;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction
{
    /// <summary>
    /// Provides a regular expression to filter and sort matching <see cref="Symbol"/>s
    /// </summary>
    internal sealed class SymbolFilter
    {
        public string SearchString;  // not a property to allow ref passing
        public Type FilterInputType {
            get => _inputType;
            set
            {
                _needsUpdate = true;
                _inputType = value;
            }
        }
        public Type FilterOutputType {
            get => _outputType;
            set
            {
                _needsUpdate = true;
                _outputType = value;
            }
        }

        public bool OnlyMultiInputs { get; set; }
        public List<SymbolUi> MatchingSymbolUis { get; private set; } = new();
        //public List<Variation> MatchingPresets { get; } = new();
        public SymbolVariationPool PresetPool { get; private set; }

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
                //UpdateConnectSlotHashes();
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
                presetFilter = null;
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
        private void UpdateConnectSlotHashes()
        {
            _sourceInputHash = 0;
            _targetInputHash = 0;

            foreach (var c in ConnectionMaker.TempConnections)
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

            MatchingSymbolUis = MatchingSymbolUis.OrderBy(s => ComputeRelevancy(s, _symbolFilterString, ""))
                                                 .Reverse()
                                                 .Take(limit)
                                                 .ToList();
        }

        private double ComputeRelevancy(SymbolUi symbolUi, string query, string currentProjectName)
        {
            float relevancy = 1;

            var symbolName = symbolUi.Symbol.Name;

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
            var count = SymbolAnalysis.InformationForSymbolIds.TryGetValue(symbolUi.Symbol.Id, out var info)
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

            if (!string.IsNullOrEmpty(symbolUi.Symbol.Namespace))
            {
                if (symbolUi.Symbol.Namespace.Contains("dx11")
                    || symbolUi.Symbol.Namespace.Contains("_"))
                    relevancy *= 0.1f;

                if (symbolUi.Symbol.Namespace.StartsWith("lib"))
                {
                    relevancy *= 3f;
                }

                if (symbolUi.Symbol.Namespace.StartsWith("examples"))
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

            // TODO: Implement
            // if (IsCompositionOperatorInNamespaceOf(symbolUi))
            // {
            //     relevancy *= 1.9;
            // }
            // else if (!IsCompositionOperatorAProjectOperator && symbolUi.Namespace.StartsWith(@"projects.") && symbolUi.Namespace.Split('.').Length == 2)
            // {
            //     relevancy *= 1.9;
            // }

            // TODO: Implement
            // Bump up operators from same namespace as current project
            // var projectName = GetProjectFromNamespace(symbolUi.Namespace);
            // if (projectName != null && projectName == currentProjectName)
            //     relevancy *= 5;

            // Bump operators with matching connections 
            var matchingConnectionsCount = 0;
            if (_sourceInputHash != 0)
            {
                foreach (var inputDefinition in symbolUi.Symbol.InputDefinitions.FindAll(i => i.DefaultValue.ValueType == FilterInputType))
                {
                        var connectionHash = _sourceInputHash * 31 + inputDefinition.Id.GetHashCode();

                        if (SymbolAnalysis.ConnectionHashCounts.TryGetValue(connectionHash, out var connectionCount))
                        {
                            //Log.Debug($" <{connectionCount}x> --> {symbolUi.Symbol.Name}");
                            matchingConnectionsCount += connectionCount;
                        }
                }
            }
            
            if (_targetInputHash != 0)
            {
                foreach (var outputDefinition in symbolUi.Symbol.OutputDefinitions.FindAll(o => o.ValueType == FilterOutputType))
                {
                    var connectionHash = outputDefinition.Id.GetHashCode() * 31 + _targetInputHash;

                    if (SymbolAnalysis.ConnectionHashCounts.TryGetValue(connectionHash, out var connectionCount))
                    {
                        //Log.Debug($"  {symbolUi.Symbol.Name} --> <{connectionCount}x>");
                        matchingConnectionsCount += connectionCount;
                    }
                }
            }

            if (matchingConnectionsCount > 0)
            {
                relevancy *= 1 + MathF.Pow(matchingConnectionsCount, 0.33f) * 4f;
                //Log.Debug($"Bump relevancy {symbolUi.Symbol.Name}  {oldRelevancy:0.00} -> {relevancy:0.00}");
            }

            return relevancy;
        }

        
        private bool _needsUpdate;
        private string _symbolFilterString;
        public string PresetFilterString;

        private Type _inputType;
        private Type _outputType;
        public bool WasUpdated;

        private static int _sourceInputHash;
        private int _targetInputHash;

        private Regex _currentRegex;
        private string _lastSearchString;
    }
}