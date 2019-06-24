using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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
    [JsonObject(MemberSerialization.OptIn)]
    public class Symbol : IDisposable
    {
        public Guid Id { get; set; }
        public string SourcePath { get; set; }
        public string Name { get; set; }
        public string Namespace { get; set; }

        public readonly List<Instance> _instancesOfSymbol = new List<Instance>();
        public readonly List<SymbolChild> Children = new List<SymbolChild>();
        public readonly List<Connection> Connections = new List<Connection>();

        /// <summary>
        /// Inputs of this symbol. input values are the default values (exist only once per symbol)
        /// </summary>
        public readonly List<InputDefinition> InputDefinitions = new List<InputDefinition>();

        public readonly List<OutputDefinition> OutputDefinitions = new List<OutputDefinition>();

        public Type InstanceType { get; set; }


        #region public API =======================================================================

        internal Symbol(Type instanceType, Guid id, IEnumerable<SymbolChild> children)
            : this(instanceType, id)
        {
            Children.AddRange(children);
        }

        public Symbol(Type instanceType, Guid id)
        {
            InstanceType = instanceType;
            Name = instanceType.Name;
            Id = id;

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
            // todo: it's probably better to first check if there's a change and only then allocate
            var oldInputDefinitions = new List<InputDefinition>(InputDefinitions);
            InputDefinitions.Clear();
            foreach (var (info, attribute) in inputs)
            {
                var alreadyExistingInput = oldInputDefinitions.FirstOrDefault(i => i.Id == attribute.Id);
                if (alreadyExistingInput != null)
                {
                    InputDefinitions.Add(alreadyExistingInput);
                    oldInputDefinitions.Remove(alreadyExistingInput);
                }
                else
                {
                    InputDefinitions.Add(CreateInputDefinition(attribute, info));
                }
            }

            // check if outputs have changed
            var outputs = (from field in instanceType.GetFields()
                           let attributes = field.GetCustomAttributes(typeof(OutputAttribute), false)
                           where attributes.Any()
                           select (field, (OutputAttribute)attributes[0])).ToArray();
            var oldOutputDefinitions = new List<OutputDefinition>(OutputDefinitions);
            OutputDefinitions.Clear();
            foreach (var (output, attribute) in outputs)
            {
                var alreadyExistingOutput = oldOutputDefinitions.FirstOrDefault(o => o.Id == attribute.Id);
                if (alreadyExistingOutput != null)
                {
                    OutputDefinitions.Add(alreadyExistingOutput);
                    oldOutputDefinitions.Remove(alreadyExistingOutput);
                }
                else
                {
                    var valueType = output.FieldType.GenericTypeArguments[0];
                    OutputDefinitions.Add(new OutputDefinition() { Id = attribute.Id, Name = output.Name, ValueType = valueType });
                }
            }

            // first remove relevant connections from instances and update symbol child input values if needed
            foreach (var instance in _instancesOfSymbol)
            {
                var parent = instance.Parent;
                var parentSymbol = parent.Symbol;
                // get all connections that belong to this instance
                var connectionsToReplace = parentSymbol.Connections.FindAll(c => c.SourceParentOrChildId == instance.Id ||
                                                                                 c.TargetParentOrChildId == instance.Id);
                foreach (var connection in connectionsToReplace)
                {
                    parentSymbol.RemoveConnection(connection);
                }

                // filter out the connections where no inputs/output exist anymore
                connectionsToReplace.RemoveAll(c => oldOutputDefinitions.FirstOrDefault(output => output.Id == c.SourceSlotId || output.Id == c.TargetSlotId) != null ||
                                                    oldInputDefinitions.FirstOrDefault(input => input.Id == c.SourceSlotId || input.Id == c.TargetSlotId) != null);

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
            }

            // now remove the old instances itself...
            foreach (var instance in _instancesOfSymbol)
            {
                instance.Parent.Children.Remove(instance);
                instance.Dispose();
            }
            _instancesOfSymbol.Clear();

            // ... and create the new ones...
            foreach (var (symbolChild, parent, _) in newInstanceSymbolChildren)
            {
                CreateAndAddNewChildInstance(symbolChild, parent);
            }

            // ... and add the connections again
            foreach (var (_, parent, connectionsToReplace) in newInstanceSymbolChildren)
            {
                foreach (var connection in connectionsToReplace)
                {
                    parent.Symbol.AddConnection(connection);
                }
            }
        }

        private static InputDefinition CreateInputDefinition(InputAttribute attribute, FieldInfo info)
        {
            // create new input definition
            if (!InputValueCreators.Entries.ContainsKey(attribute.Type))
            {
                int bla = 12;
            }
            InputValue defaultValue = InputValueCreators.Entries[attribute.Type](attribute);
            var isMultiInput = info.FieldType.GetGenericTypeDefinition() == typeof(MultiInputSlot<>);
            return new InputDefinition { Id = attribute.Id, Name = info.Name, DefaultValue = defaultValue, IsMultiInput = isMultiInput };
        }

        public Instance CreateInstance(Guid id)
        {
            var newInstance = Activator.CreateInstance(InstanceType) as Instance;
            Debug.Assert(newInstance != null);
            newInstance.Id = id;
            newInstance.Symbol = this;

            int numInputs = newInstance.Inputs.Count;
            for (int i = 0; i < numInputs; i++)
            {
                newInstance.Inputs[i].Id = InputDefinitions[i].Id;
            }

            int numOutputs = newInstance.Outputs.Count;
            for (int i = 0; i < numOutputs; i++)
            {
                newInstance.Outputs[i].Id = OutputDefinitions[i].Id;
            }

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
            var childInstance = childSymbol.CreateInstance(symbolChild.Id);
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
            var existingConnection = Connections.FirstOrDefault(c => c.TargetParentOrChildId == connection.TargetParentOrChildId &&
                                                                     c.TargetSlotId == connection.TargetSlotId);
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
            var newChild = new SymbolChild(symbol, Guid.NewGuid());
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
            return Connections.FirstOrDefault(c => c.TargetParentOrChildId == input.Id);
        }

        public Connection GetConnectionForOutput(OutputDefinition output)
        {
            return Connections.FirstOrDefault(c => c.SourceParentOrChildId == output.Id);
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
            public bool IsMultiInput { get; set; }
        }

        public class OutputDefinition
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public Type ValueType { get; set; }
        }

        public class Connection
        {
            public Guid SourceParentOrChildId { get; }
            public Guid SourceSlotId { get; }
            public Guid TargetParentOrChildId { get; }
            public Guid TargetSlotId { get; }

            public Connection(Guid sourceParentOrChildId, Guid sourceSlotId, Guid targetSymbolChildId, Guid targetSlotId)
            {
                SourceParentOrChildId = sourceParentOrChildId;
                SourceSlotId = sourceSlotId;
                TargetParentOrChildId = targetSymbolChildId;
                TargetSlotId = targetSlotId;
            }

            public override int GetHashCode()
            {
                int hash = SourceParentOrChildId.GetHashCode();
                hash = hash * 31 + SourceSlotId.GetHashCode();
                hash = hash * 31 + TargetParentOrChildId.GetHashCode();
                hash = hash * 31 + TargetSlotId.GetHashCode();
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