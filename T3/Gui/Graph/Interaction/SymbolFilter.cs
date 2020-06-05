using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Gui.Graph.Interaction
{
    /// <summary>
    /// Provides a regular expression to filter and sort matching <see cref="Symbol"/>s
    /// </summary>
    public class SymbolFilter
    {
        public string SearchString; // not a property to allow ref passing
        public Type FilterInputType { get; set; }
        public Type FilterOutputType { get; set; }

        public void UpdateIfNecessary()
        {
            var needsUpdate = false;

            if (_currentSearchString != SearchString)
            {
                _currentSearchString = SearchString;
                var pattern = string.Join(".*", _currentSearchString.ToCharArray());
                try
                {
                    _currentRegex = new Regex(pattern, RegexOptions.IgnoreCase);    
                }
                catch(ArgumentException)
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
                UpdateMatchingSymbols();
            }

            WasUpdated = needsUpdate;
        }

        private Type _inputType;
        private Type _outputType;
        public bool WasUpdated;

        private void UpdateMatchingSymbols()
        {
            var parentSymbols = new List<Symbol>(GraphCanvas.Current.GetParentSymbols());

            MatchingSymbolUis.Clear();

            foreach (var symbolUi in SymbolUiRegistry.Entries.Values)
            {
                if (parentSymbols.Contains(symbolUi.Symbol))
                    continue;

                if (_inputType != null)
                {
                    var matchingInputDef = symbolUi.Symbol.GetInputMatchingType(FilterInputType);
                    if (matchingInputDef == null)
                        continue;
                }

                if (_outputType != null)
                {
                    var matchingOutputDef = symbolUi.Symbol.GetOutputMatchingType(FilterOutputType);
                    if (matchingOutputDef == null)
                        continue;
                }

                if (!_currentRegex.IsMatch(symbolUi.Symbol.Name))
                    continue;

                MatchingSymbolUis.Add(symbolUi);
            }

            MatchingSymbolUis = MatchingSymbolUis.OrderBy(s => ComputeRelevancy(s, _currentSearchString, "")).Reverse().Take(30).ToList();
        }



        private static double ComputeRelevancy(SymbolUi symbolUi, string query, string currentProjectName)
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
                    relevancy *= 3;
                }
            }
            
            // Add usage count (the following statement is slow and should be cached)
            var count = symbolUi.Symbol.InstancesOfSymbol.Select(instance =>instance.SymbolChildId).Distinct().Count();
            relevancy *= 1 + count/100f;
            
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

            // Bump up if query occurs in description
            if (query.Length > 2)
            {
                if (symbolUi.Description != null && symbolUi.Description.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    relevancy *= 1.2f;
                }
            }
            
            if (!string.IsNullOrEmpty(symbolUi.Symbol.Namespace))
            {
                if (symbolUi.Symbol.Namespace.Contains("dx11"))
                    relevancy *= 0.10f;

                // TODO: Implement
                // if (symbolUi.InstanceCount > 0)
                // {
                //     relevancy *= -0.5 / symbolUi.InstanceCount + 2.0;
                // }

                //relevancy *= 2 - 1.0 / (0.3 * _numberOfMetaOperatorUsage[symbolUi.ID] + 0.7);

                //relevancy *= (1 + (1.0 / (op.Name.Length + op.Namespace.Length)) * 0.05);

                if (Regex.Match(symbolUi.Symbol.Namespace, @"^lib\..*", RegexOptions.IgnoreCase) != Match.Empty)
                {
                    relevancy *= 1.6f;
                }
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

            return relevancy;
        }
        

        public List<SymbolUi> MatchingSymbolUis { get; private set; } = new List<SymbolUi>();

        private Regex _currentRegex;
        private string _currentSearchString;
    }
}