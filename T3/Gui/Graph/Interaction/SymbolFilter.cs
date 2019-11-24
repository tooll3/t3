using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using T3.Core.Operator;

namespace T3.Gui.Graph.Interaction
{
    /// <summary>
    /// Provides a regular expression to filter for matching <see cref="Symbol"/>s
    /// </summary>
    public class SymbolFilter
    {
        public string SearchString; // not a property to allow ref passing
        public Type FilterInputType { get; set; }
        public Type FilterOutputType { get; set; }

        public void Update()
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

            MatchingSymbols = new List<Symbol>();

            foreach (var symbol in SymbolRegistry.Entries.Values)
            {
                if (parentSymbols.Contains(symbol))
                    continue;

                if (_inputType != null)
                {
                    var matchingInputDef = GetInputMatchingType(symbol, FilterInputType);
                    if (matchingInputDef == null)
                        continue;
                }

                if (_outputType != null)
                {
                    var matchingOutputDef = GetOutputMatchingType(symbol, FilterOutputType);
                    if (matchingOutputDef == null)
                        continue;
                }

                if (!_currentRegex.IsMatch(symbol.Name))
                    continue;

                MatchingSymbols.Add(symbol);
            }
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

        public List<Symbol> MatchingSymbols { private set; get; }

        private Regex _currentRegex;
        private string _currentSearchString;
    }
}