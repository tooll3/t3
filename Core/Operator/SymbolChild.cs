using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator
{
    using InputDefinitionId = Guid;

    /// <summary>
    /// Represents an instance of a <see cref="Symbol"/> within a combined symbol group.
    /// </summary>
    public class SymbolChild
    {
        /// <summary>A reference to the <see cref="Symbol"/> this is an instance from.</summary>
        public Symbol Symbol { get; }

        public Guid Id { get; }

        public string Name { get; set; } = string.Empty;

        public string ReadableName => string.IsNullOrEmpty(Name) ? Symbol.Name : Name;

        public Dictionary<InputDefinitionId, Input> InputValues { get; } = new Dictionary<InputDefinitionId, Input>();
        public Dictionary<Guid, IOutputData> OutputData { get; } = new Dictionary<Guid, IOutputData>();

        public SymbolChild(Symbol symbol, Guid childId)
        {
            Symbol = symbol;
            Id = childId;

            foreach (var inputDefinition in symbol.InputDefinitions)
            {
                InputValues.Add(inputDefinition.Id, new Input(inputDefinition));
            }

            foreach (var outputDefinition in symbol.OutputDefinitions)
            {
                if (outputDefinition.OutputDataType != null)
                {
                    OutputData.Add(outputDefinition.Id, Activator.CreateInstance(outputDefinition.OutputDataType) as IOutputData);
                }
            }
        }

        #region sub classes =============================================================

        public class Input
        {
            public Symbol.InputDefinition InputDefinition { get; }
            public InputValue DefaultValue => InputDefinition.DefaultValue;

            public string Name => InputDefinition.Name;

            /// <summary>The input value used for this symbol child</summary>
            public InputValue Value { get; }

            public bool IsDefault { get; set; }

            public Input(Symbol.InputDefinition inputDefinition)
            {
                InputDefinition = inputDefinition;
                Value = DefaultValue.Clone();
                IsDefault = true;
            }

            public void SetCurrentValueAsDefault()
            {
                DefaultValue.Assign(Value);
                IsDefault = true;
            }

            public void ResetToDefault()
            {
                Value.Assign(DefaultValue);
                IsDefault = true;
            }
        }

        #endregion
    }
}
