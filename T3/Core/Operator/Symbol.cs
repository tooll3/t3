using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

//using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace T3.Core.Operator
{
    /// <summary>
    /// Represents the definition of an operator. It can include:
    /// - <see cref="SymbolChild"/>s that references other Symbols
    /// - <see cref="Connection"/>s that connect these children
    /// </summary>
    /// <remarks>
    /// - There can be multiple <see cref="Instance"/>s of a symbol.
    /// </remarks>
    public class Symbol : IDisposable
    {
        public Guid Id { get; set; }
        public string SourcePath { get; set; }
        public string SymbolName { get; set; }
        public string Namespace { get; set; }

        private readonly List<Instance> _instancesOfSymbol = new List<Instance>();
        public readonly List<SymbolChild> Children = new List<SymbolChild>();
        public readonly List<Connection> Connections = new List<Connection>();

        /// <summary>
        /// Inputs of this symbol. input values are the default values (exist only once per symbol)
        /// </summary>
        public readonly List<InputDefinition> InputDefinitions = new List<InputDefinition>();

        public readonly List<OutputDefinition> OutputDefinitions = new List<OutputDefinition>();

        public Type InstanceType { get; set; }


        #region public API =======================================================================

        public Symbol(Type instanceType)
        {
            InstanceType = instanceType;
            SymbolName = instanceType.Name;
            Id = Guid.NewGuid();

            // input identified by base interface
            Type inputSlotType = typeof(IInputSlot);
            var inputInfos = from field in instanceType.GetFields()
                             where inputSlotType.IsAssignableFrom(field.FieldType)
                             select field;
            foreach (var inputInfo in inputInfos)
            {
                var customAttributes = inputInfo.GetCustomAttributes(typeof(InputAttribute), false);
                Debug.Assert(customAttributes.Length == 1);
                var attribute = (InputAttribute)customAttributes[0];
                InputValue defaultValue = null;
                if (attribute is IntInputAttribute intAttribute)
                {
                    defaultValue = new InputValue<int>(intAttribute.DefaultValue);
                }
                else if (attribute is FloatInputAttribute floatAttribute)
                {
                    defaultValue = new InputValue<float>(floatAttribute.DefaultValue);
                }
                else if (attribute is StringInputAttribute stringAttribute)
                {
                    defaultValue = new InputValue<string>(stringAttribute.DefaultValue);
                }
                else
                {
                    Debug.Assert(false);
                }
                InputDefinitions.Add(new InputDefinition() { Id = Guid.NewGuid(), Name = inputInfo.Name, DefaultValue = defaultValue });
            }

            // outputs identified by attribute
            var outputs = (from field in instanceType.GetFields()
                           let attributes = field.GetCustomAttributes(typeof(OutputAttribute), false)
                           from attr in attributes
                           select field).ToArray();
            foreach (var output in outputs)
            {
                var valueType = output.FieldType.GenericTypeArguments[0];
                OutputDefinitions.Add(new OutputDefinition() { Id = Guid.NewGuid(), Name = output.Name, ValueType = valueType });
            }
        }

        public void Dispose()
        {
            _instancesOfSymbol.ForEach(instance => instance.Dispose());
        }

        public Instance CreateInstance()
        {
            var newInstance = Activator.CreateInstance(InstanceType) as Instance;
            Debug.Assert(newInstance != null);
            newInstance.Symbol = this;

            // create child instances
            foreach (var symbolChild in Children)
            {
                CreateAndAddNewChildInstance(symbolChild, newInstance);
            }

            // create connections between instances
            foreach (var connection in Connections)
            {
                newInstance.AddConnection(connection);
            }

            _instancesOfSymbol.Add(newInstance);

            return newInstance;
        }

        private static void CreateAndAddNewChildInstance(SymbolChild symbolChild, Instance parentInstance)
        {
            var childSymbol = symbolChild.Symbol;
            var childInstance = childSymbol.CreateInstance();
            childInstance.Id = symbolChild.Id;
            childInstance.Parent = parentInstance;

            // set up the inputs for the child instance
            for (int i = 0; i < symbolChild.Symbol.InputDefinitions.Count; i++)
            {
                Debug.Assert(i < childInstance.Inputs.Count);
                Guid inputDefinitionId = childSymbol.InputDefinitions[i].Id;
                childInstance.Inputs[i].Input = symbolChild.InputValues[inputDefinitionId];
                childInstance.Inputs[i].Id = inputDefinitionId;
            }

            // set up the outputs for the child instance
            for (int i = 0; i < symbolChild.Symbol.OutputDefinitions.Count; i++)
            {
                Debug.Assert(i < childInstance.Outputs.Count);
                childInstance.Outputs[i].Id = childSymbol.OutputDefinitions[i].Id;
            }

            parentInstance.Children.Add(childInstance);
        }

        public void AddConnection(Connection connection)
        {
            // check if another connection is already existing to the target input, ignoring multi inputs for now
            var existingConnection = Connections.FirstOrDefault(c => c.TargetChildId == connection.TargetChildId &&
                                                                     c.InputDefinitionId == connection.InputDefinitionId);
            if (existingConnection != null)
            {
                RemoveConnection(existingConnection);
            }

            Connections.Add(connection);
            foreach (var instance in _instancesOfSymbol)
            {
                instance.AddConnection(connection);
            }
        }

        public void RemoveConnection(Connection connection)
        {
            var index = Connections.FindIndex(storedConnection => storedConnection.Equals(connection));
            if (index != -1)
            {
                Connections.RemoveAt(index);
                foreach (var instance in _instancesOfSymbol)
                {
                    instance.RemoveConnection(connection);
                }
            }
        }

        public Guid AddChild(Symbol symbol)
        {
            var newChild = new SymbolChild(symbol);
            Children.Add(newChild);

            foreach (var instance in _instancesOfSymbol)
            {
                CreateAndAddNewChildInstance(newChild, instance);
            }

            return newChild.Id;
        }

        void DeleteInstance(Instance op)
        {
            _instancesOfSymbol.Remove(op);
        }

        public Connection GetConnectionForInput(InputDefinition input)
        {
            return Connections.FirstOrDefault(c => c.TargetChildId == input.Id);
        }

        public Connection GetConnectionForOutput(OutputDefinition output)
        {
            return Connections.FirstOrDefault(c => c.SourceChildId == output.Id);
        }
    
        #endregion


        #region sub classses =============================================================================

        /// <summary>
        /// Options on the visual presentation of <see cref="Symbol"/> input.
        /// </summary>
        public class InputDefinition
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public InputValue DefaultValue { get; set; }

            public Relevancy Relevance;
            public enum Relevancy
            {
                Required,
                Relevant,
                Optional
            }

            //TODO: how do we handle MultiInputs?
        }

        public class OutputDefinition
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public Type ValueType { get; set; }
        }

        public class Connection
        {
            public Guid SourceChildId { get; }
            public Guid OutputDefinitionId { get; }
            public Guid TargetChildId { get; }
            public Guid InputDefinitionId { get; }

            public Connection(Guid sourceChildId, Guid outputDefinitionId, Guid targetChildId, Guid inputDefinitionId)
            {
                SourceChildId = sourceChildId;
                OutputDefinitionId = outputDefinitionId;
                TargetChildId = targetChildId;
                InputDefinitionId = inputDefinitionId;
            }

            public override int GetHashCode()
            {
                int hash = SourceChildId.GetHashCode();
                hash = hash*31 + OutputDefinitionId.GetHashCode();
                hash = hash*31 + TargetChildId.GetHashCode();
                hash = hash*31 + InputDefinitionId.GetHashCode();
                return hash;
            }

            public override bool Equals(object other)
            {
                return GetHashCode() == other?.GetHashCode();
            }

        }
        #endregion
    }
}