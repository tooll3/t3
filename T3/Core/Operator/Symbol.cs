using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace T3.Core.Operator
{
    public class Symbol : IDisposable
    {
        public Guid Id { get; set; }
        public string SourcePath { get; set; }
        public string SymbolName { get; set; }
        public string Namespace { get; set; }

        public Symbol()
        {
        }

        public class Connection
        {
            public Guid SourceInstanceId { get; }
            public Guid SourceSlotId { get; }
            public Guid TargetInstanceId { get; }
            public Guid TargetSlotId { get; }

            public Connection(Guid sourceInstanceId, Guid sourceSlotId, Guid targetInstanceId, Guid targetSlotId)
            {
                SourceInstanceId = sourceInstanceId;
                SourceSlotId = sourceSlotId;
                TargetInstanceId = targetInstanceId;
                TargetSlotId = targetSlotId;
            }
        }

        public Instance CreateInstance()
        {
            var newInstance = Activator.CreateInstance(InstanceType) as Instance;
            Debug.Assert(newInstance != null);
            newInstance.Symbol = this;

            // Link inputs to default input values
            for (int i = 0; i < InputDefinitions.Count; i++)
            {
                Debug.Assert(i < newInstance.Inputs.Count);
                if (newInstance.Inputs[i] is IInputSlot input)
                {
                    input.InputValue = InputDefinitions[i].DefaultValue;
                }
            }

            // create children
            foreach (var childInstanceDef in _children)
            {
                var childInstance = childInstanceDef.Symbol.CreateInstance();
                childInstance.Id = childInstanceDef.Id;
                childInstance.Parent = newInstance;

                newInstance.Children.Add(childInstance);
            }

            _instancesOfSymbol.Add(newInstance);

            return newInstance;
        }

        public Guid AddChild(Symbol symbol)
        {
            var newChild = new SymbolChild(symbol);
            _children.Add(newChild);

            foreach (var instance in _instancesOfSymbol)
            {
                var childInstance = symbol.CreateInstance();
                childInstance.Id = newChild.Id;
                childInstance.Parent = instance;

                instance.Children.Add(childInstance);
            }

            return newChild.Id;
        }

        void DeleteInstance(Instance op)
        {
            _instancesOfSymbol.Remove(op);
        }

//         InputValue GetInputValue(Guid childInstanceId, Guid inputId)
//         {
//             var inputValue = (from child in _children
//                               where child.Id == childInstanceId
//                               from input in child.InputValues
//                               where input.Key == inputId
//                               select input.Value).Single();
//             return inputValue;
//         }
// 
//         InputValue GetInputDefaultValue(Guid inputId)
//         {
//             var inputDefaultValue = (from input in InputDefinitions
//                                      where input.Id == inputId
//                                      select input.DefaultValue).Single();
//             return inputDefaultValue;
//         }

        public readonly List<Instance> _instancesOfSymbol = new List<Instance>();
        public readonly List<Connection> _connections = new List<Connection>();
        public readonly List<SymbolChild> _children = new List<SymbolChild>();

        // Inputs of this symbol, input values are the default values (exist only once per symbol)
        public readonly List<InputDefinition> InputDefinitions = new List<InputDefinition>();
        public Type InstanceType { get; set; }

        public void Dispose()
        {
            _instancesOfSymbol.ForEach(instance => instance.Dispose());
        }
    }

    public class SymbolChild
    {
        public class Input
        {
            public Input(InputValue defaultValue)
            {
                DefaultValue = defaultValue;
                Value = defaultValue.Clone();
                IsDefault = true;
            }

            // reference to the default value defined in corresponding symbol
            public InputValue DefaultValue { get; }
            // the input value used for this symbol child
            public InputValue Value { get; }
            public bool IsDefault { get; set; }

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

        public SymbolChild(Symbol symbol)
        {
            Symbol = symbol;
            Id = Guid.NewGuid();
            foreach (var symbolInput in symbol.InputDefinitions)
            {
                InputValues.Add(symbolInput.Id, new Input(symbolInput.DefaultValue));
            }
        }

        public Symbol Symbol { get; }
        public Guid Id { get; }
        // map input id to actual input value
        public Dictionary<Guid, Input> InputValues { get; } = new Dictionary<Guid, Input>();
    }

    public class InputDefinition
    {
        // relevance: required, relevant, optional
        public Guid Id { get; set; }
        public string Name { get; set; }
        public InputValue DefaultValue { get; set; }
    }
}