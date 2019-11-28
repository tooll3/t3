using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
                _currentRegex = new Regex(pattern, RegexOptions.IgnoreCase);
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
        }

        private Type _inputType;
        private Type _outputType;

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
                    var matchingInputDef = GetInputMatchingType(symbolUi.Symbol, FilterInputType);
                    if (matchingInputDef == null)
                        continue;
                }

                if (_outputType != null)
                {
                    var matchingOutputDef = GetOutputMatchingType(symbolUi.Symbol, FilterOutputType);
                    if (matchingOutputDef == null)
                        continue;
                }

                if (!_currentRegex.IsMatch(symbolUi.Symbol.Name))
                    continue;

                MatchingSymbolUis.Add(symbolUi);
            }
            MatchingSymbolUis = MatchingSymbolUis.OrderBy(s=> ComputeRelevancy(s, _currentSearchString, "")).Reverse().Take(30).ToList();
            // let rating = ComputeRelevancy(metaOpEntry.Value, XSearchTextBox.Text, currentProjectName)
            // orderby rating
        }

        public Symbol.InputDefinition GetInputMatchingType(Symbol symbol, Type type)
        {
            foreach (var inputDefinition in symbol.InputDefinitions)
            {
                if (type == null || inputDefinition.DefaultValue.ValueType == type)
                    return inputDefinition;
            }

            return null;
        }

        public Symbol.OutputDefinition GetOutputMatchingType(Symbol symbol, Type type)
        {
            foreach (var outputDefinition in symbol.OutputDefinitions)
            {
                if (type == null || outputDefinition.ValueType == type)
                    return outputDefinition;
            }

            return null;
        }

        
        private double ComputeRelevancy(SymbolUi symbolUi, string query, string currentProjectName)
        {
            double relevancy = 1;

            if (symbolUi.Symbol.Name.Equals(query, StringComparison.InvariantCultureIgnoreCase))
            {
                relevancy *= 5;
            }

            if (symbolUi.Symbol.Name.StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
            {
                relevancy *= 4.5;
            }
            else
            {
                if (symbolUi.Symbol.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    relevancy *= 3;
                }
            }

            // Bump up if query occurs in description
            if (query.Length > 2)
            {
                if (symbolUi.Description != null && symbolUi.Description.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    relevancy *= 1.2;
                }
            }

            // if (symbolUi.Symbol.Name == "Time")      // disfavor shadow ops
            //     relevancy *= 0.3;
            //
            // if (symbolUi.Symbol.Name == "Curve")      // disfavor shadow ops
            //     relevancy *= 0.05;
            
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
                relevancy *= 1.6;
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

        // private bool IsCompositionOperatorInNamespaceOf(MetaOperator op)
        // {
        //     return op.Namespace.StartsWith(_compositionOperator.Definition.Namespace);
        // }
        //
        // private void InitDictionaryWithMetaOperatorUsageCount()
        // {
        //     foreach (var metaOp in App.Current.Model.MetaOpManager.MetaOperators)
        //     {
        //         _numberOfMetaOperatorUsage.Add(metaOp.Key, 1);
        //     }
        //     CountUsageOfMetaOps();
        // }

        // private void CountUsageOfMetaOps()
        // {
        //     foreach (var metaOp in App.Current.Model.MetaOpManager.MetaOperators)
        //     {
        //         foreach (var internalIds in metaOp.Value.InternalOperatorsMetaOpId)
        //         {
        //             if (_numberOfMetaOperatorUsage.ContainsKey(internalIds))
        //             {
        //                 _numberOfMetaOperatorUsage[internalIds]++;
        //             }
        //         }
        //     }
        // }
        
        
        
        public  List<SymbolUi> MatchingSymbolUis { get; private set; } = new List<SymbolUi>();

        private Regex _currentRegex;
        private string _currentSearchString;
    }
}