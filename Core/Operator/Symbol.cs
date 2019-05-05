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
            var inputInfos = instanceType.GetFields().Where(f => inputSlotType.IsAssignableFrom(f.FieldType));
            foreach (var inputInfo in inputInfos)
            {
                var customAttributes = inputInfo.GetCustomAttributes(typeof(InputAttribute), false);
                Debug.Assert(customAttributes.Length == 1);
                var attribute = (InputAttribute)customAttributes[0];
                InputDefinitions.Add(CreateInputDefinition(attribute, inputInfo));
            }

            // outputs identified by attribute
            var outputs = (from field in instanceType.GetFields()
                           let attributes = field.GetCustomAttributes(typeof(OutputAttribute), false)
                           from attr in attributes
                           select (field, attributes)).ToArray();
            foreach (var (output, attributes) in outputs)
            {
                var valueType = output.FieldType.GenericTypeArguments[0];
                var attribute = (OutputAttribute)attributes.First();
                OutputDefinitions.Add(new OutputDefinition() { Id = attribute.Id, Name = output.Name, ValueType = valueType });
            }
        }

        public void Dispose()
        {
            _instancesOfSymbol.ForEach(instance => instance.Dispose());
        }

        public void SetInstanceType(Type instanceType)
        {
            InstanceType = instanceType;
            List<(SymbolChild, Instance, List<Connection>)> newInstanceSymbolChildren = new List<(SymbolChild, Instance, List<Connection>)>();

            // check if inputs have changed
            Type inputSlotType = typeof(IInputSlot);
            var inputInfos = instanceType.GetFields().Where(f => inputSlotType.IsAssignableFrom(f.FieldType));
            var inputs = (from inputInfo in inputInfos
                                   let customAttributes = inputInfo.GetCustomAttributes(typeof(InputAttribute), false)
                                   where customAttributes.Any()
                                   select (inputInfo, (InputAttribute)customAttributes[0])).ToArray();
            var oldInputDefinitions = new List<InputDefinition>(InputDefinitions);
            InputDefinitions.Clear();
            foreach (var (info, attribute) in inputs)
            {
                var alreadyExistingInput = oldInputDefinitions.FirstOrDefault(i => i.Id == attribute.Id);
                InputDefinitions.Add(alreadyExistingInput ?? CreateInputDefinition(attribute, info));
            }

            // check if outputs have changed


            foreach (var instance in _instancesOfSymbol)
            {
                var parent = instance.Parent;
                var parentSymbol = parent.Symbol;
                // get all connections that belong to this instance
                var connectionsToReplace = parentSymbol.Connections.FindAll(c => c.SourceChildId == instance.Id ||
                                                                                 c.TargetChildId == instance.Id);
                foreach (var connection in connectionsToReplace)
                {
                    parent.RemoveConnection(connection);
                }

                var symbolChild = parentSymbol.Children.Single(child => child.Id == instance.Id);

                // update inputs of symbol child
                var oldChildInputs = new Dictionary<Guid, SymbolChild.Input>(symbolChild.InputValues);
                symbolChild.InputValues.Clear();
                foreach (var inputDefinition in InputDefinitions)
                {
                    if (oldChildInputs.TryGetValue(inputDefinition.Id, out var input))
                    {
                        symbolChild.InputValues.Add(inputDefinition.Id, input);
                    }
                    else
                    {
                        symbolChild.InputValues.Add(inputDefinition.Id, new SymbolChild.Input(inputDefinition));
                    }
                }

                newInstanceSymbolChildren.Add((symbolChild, parent, connectionsToReplace));

                parent.Children.Remove(instance);
                instance.Dispose();
            }

            _instancesOfSymbol.Clear();
            foreach (var (symbolChild, parent, connectionsToReplace) in newInstanceSymbolChildren)
            {
                CreateAndAddNewChildInstance(symbolChild, parent);
                foreach (var connection in connectionsToReplace)
                {
                    parent.AddConnection(connection);
                }
            }
        }

        private static InputDefinition CreateInputDefinition(InputAttribute attribute, FieldInfo info)
        {
            // create new input definition
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

            return new InputDefinition() { Id = attribute.Id, Name = info.Name, DefaultValue = defaultValue };
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
                                                                     c.TargetDefinitionId == connection.TargetDefinitionId);
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
            public Guid SourceDefinitionId { get; }
            public Guid TargetChildId { get; }
            public Guid TargetDefinitionId { get; }

            public Connection(Guid sourceChildId, Guid sourceDefinitionId, Guid targetChildId, Guid targetDefinitionId)
            {
                SourceChildId = sourceChildId;
                SourceDefinitionId = sourceDefinitionId;
                TargetChildId = targetChildId;
                TargetDefinitionId = targetDefinitionId;
            }

            public override int GetHashCode()
            {
                int hash = SourceChildId.GetHashCode();
                hash = hash*31 + SourceDefinitionId.GetHashCode();
                hash = hash*31 + TargetChildId.GetHashCode();
                hash = hash*31 + TargetDefinitionId.GetHashCode();
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