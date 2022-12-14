using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Editor.Gui.Graph.Interaction
{
    /// <summary>
    /// Provides a regular expression to filter and sort matching <see cref="Symbol"/>s
    /// </summary>
    public class SymbolFilter
    {
        public string SearchString; // not a property to allow ref passing
        public Type FilterInputType { get; set; }
        public Type FilterOutputType { get; set; }
        public bool OnlyMultiInputs { get; set; }

        public void UpdateIfNecessary(bool forceUpdate = false)
        {
            var needsUpdate = forceUpdate;
            
            if (_currentSearchString != SearchString)
            {
                _currentSearchString = SearchString;
                var pattern = string.Join(".*", _currentSearchString.ToCharArray());
                try
                {
                    _currentRegex = new Regex(pattern, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException)
                {
                    Log.Debug("Invalid Regex format: " + pattern);
                    return;
                }

                needsUpdate = true;
            }

            if (_inputType != FilterInputType)
            {
                _inputType = FilterInputType;
                needsUpdate = true;
            }

            if (_outputType != FilterOutputType)
            {
                _outputType = FilterOutputType;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                UpdateConnectSlotHashes();
                UpdateMatchingSymbols();
            }

            WasUpdated = needsUpdate;
        }

        private Type _inputType;
        private Type _outputType;
        public bool WasUpdated;

        private static int _sourceInputHash;
        private int _targetInputHash;

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

        private void UpdateMatchingSymbols()
        {
            var composition = NodeSelection.GetSelectedComposition();
            var parentSymbolIds = composition != null
                                      ? new HashSet<Guid>(NodeOperations.GetParentInstances(composition, includeChildInstance: true).Select(p => p.Symbol.Id))
                                      : new HashSet<Guid>();

            MatchingSymbolUis.Clear();
            foreach (var symbolUi in SymbolUiRegistry.Entries.Values)
            {
                // Prevent graph cycles
                if (parentSymbolIds.Contains(symbolUi.Symbol.Id))
                    continue;

                if (_inputType != null)
                {
                    var matchingInputDef = symbolUi.Symbol.GetInputMatchingType(FilterInputType);
                    if (matchingInputDef == null)
                        continue;

                    if (OnlyMultiInputs && !matchingInputDef.IsMultiInput)
                        continue;
                }

                if (_outputType != null)
                {
                    var matchingOutputDef = symbolUi.Symbol.GetOutputMatchingType(FilterOutputType);
                    if (matchingOutputDef == null)
                        continue;
                }

                if (!(_currentRegex.IsMatch(symbolUi.Symbol.Name)
                      || symbolUi.Symbol.Namespace.Contains(SearchString, StringComparison.InvariantCultureIgnoreCase)
                      || (!string.IsNullOrEmpty(symbolUi.Description)
                          && symbolUi.Description.Contains(SearchString, StringComparison.InvariantCultureIgnoreCase))))
                    continue;

                MatchingSymbolUis.Add(symbolUi);
            }

            MatchingSymbolUis = MatchingSymbolUis.OrderBy(s => ComputeRelevancy(s, _currentSearchString, ""))
                                                 .Reverse()
                                                 .Take(30)
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
                relevancy *= 1.5f;
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
                relevancy *= 2.5f;
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

        public List<SymbolUi> MatchingSymbolUis { get; private set; } = new List<SymbolUi>();

        private Regex _currentRegex;
        private string _currentSearchString;
    }
}