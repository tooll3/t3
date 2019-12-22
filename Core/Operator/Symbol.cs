using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using T3.Core.Logging;

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
        public string Name { get; set; }
        public string Namespace { get; set; }

        public readonly List<Instance> InstancesOfSymbol = new List<Instance>();
        public readonly List<SymbolChild> Children = new List<SymbolChild>();
        public readonly List<Connection> Connections = new List<Connection>();

        /// <summary>
        /// Inputs of this symbol. input values are the default values (exist only once per symbol)
        /// </summary>
        public readonly List<InputDefinition> InputDefinitions = new List<InputDefinition>();

        public readonly List<OutputDefinition> OutputDefinitions = new List<OutputDefinition>();

        public Type InstanceType { get; set; }

        public Animator Animator { get; } = new Animator();

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
                var isMultiInput = inputInfo.FieldType.GetGenericTypeDefinition() == typeof(MultiInputSlot<>);
                var valueType = inputInfo.FieldType.GetGenericArguments()[0];
                var inputDef = CreateInputDefinition(attribute.Id, inputInfo.Name, isMultiInput, valueType);
                InputDefinitions.Add(inputDef);
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
            InstancesOfSymbol.ForEach(instance => instance.Dispose());
        }

        private class ConnectionEntry
        {
            public Connection Connection { get; set; }
            public int MultiInputIndex { get; set; }
        }

        public void SetInstanceType(Type instanceType)
        {
            InstanceType = instanceType;
            Name = instanceType.Name;
            Log.Info($"new instance type name: {Name}");
            var newInstanceSymbolChildren = new List<(SymbolChild, Instance, List<ConnectionEntry>)>();

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
                    alreadyExistingInput.Name = info.Name;
                    alreadyExistingInput.IsMultiInput = info.FieldType.GetGenericTypeDefinition() == typeof(MultiInputSlot<>);
                    InputDefinitions.Add(alreadyExistingInput);
                    oldInputDefinitions.Remove(alreadyExistingInput);
                }
                else
                {
                    var isMultiInput = info.FieldType.GetGenericTypeDefinition() == typeof(MultiInputSlot<>);
                    var valueType = info.FieldType.GetGenericArguments()[0];
                    var inputDef = CreateInputDefinition(attribute.Id, info.Name, isMultiInput, valueType);
                    InputDefinitions.Add(inputDef);
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

            var connectionsToRemoveWithinSymbol = new List<Connection>();
            foreach (var con in Connections)
            {
                foreach (var input in oldInputDefinitions)
                {
                    if (con.SourceSlotId == input.Id)
                        connectionsToRemoveWithinSymbol.Add(con);
                }

                foreach (var output in oldOutputDefinitions)
                {
                    if (con.TargetSlotId == output.Id)
                        connectionsToRemoveWithinSymbol.Add(con);
                }
            }

            connectionsToRemoveWithinSymbol = connectionsToRemoveWithinSymbol.Distinct().ToList(); // remove possible duplicates
            connectionsToRemoveWithinSymbol.Reverse(); // reverse order to have always valid multi input indices
            var connectionEntriesToRemove = new List<ConnectionEntry>(connectionsToRemoveWithinSymbol.Count);
            foreach (var con in connectionsToRemoveWithinSymbol)
            {
                    var entry = new ConnectionEntry
                                {
                                    Connection = con,
                                    MultiInputIndex = Connections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
                                                                               && c.TargetSlotId == con.TargetSlotId)
                                                                 .FindIndex(cc => cc == con) // todo: fix this mess! connection rework!
                                };
                    connectionEntriesToRemove.Add(entry);
            }

            foreach (var entry in connectionEntriesToRemove)
            {
                RemoveConnection(entry.Connection, entry.MultiInputIndex);
            }

            // first remove relevant connections from instances and update symbol child input values if needed
            foreach (var instance in InstancesOfSymbol)
            {
                var parent = instance.Parent;
                var parentSymbol = parent.Symbol;
                // get all connections that belong to this instance
                var connectionsToReplace = parentSymbol.Connections.FindAll(c => c.SourceParentOrChildId == instance.SymbolChildId ||
                                                                                 c.TargetParentOrChildId == instance.SymbolChildId);
                // first remove those connections where the inputs/outputs doesn't exist anymore
                var connectionsToRemove =
                    connectionsToReplace.FindAll(c =>
                                                 {
                                                     return oldOutputDefinitions.FirstOrDefault(output => output.Id == c.SourceSlotId || output.Id == c.TargetSlotId) != null ||
                                                            oldInputDefinitions.FirstOrDefault(input => input.Id == c.SourceSlotId || input.Id == c.TargetSlotId) != null;
                                                 });

                foreach (var connection in connectionsToRemove)
                {
                    parentSymbol.RemoveConnection(connection);
                    connectionsToReplace.Remove(connection);
                }

                // now create the entries for those that will be reconnected after the instance has been replaced. Take care of the multi input order
                connectionsToReplace.Reverse();
                var connectionEntriesToReplace = new List<ConnectionEntry>(connectionsToReplace.Count);
                foreach (var con in connectionsToReplace)
                {
                    var entry = new ConnectionEntry
                                {
                                    Connection = con,
                                    MultiInputIndex = parentSymbol.Connections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
                                                                                            && c.TargetSlotId == con.TargetSlotId)
                                                                  .FindIndex(cc => cc == con) // todo: fix this mess! connection rework!
                                };
                    connectionEntriesToReplace.Add(entry);
                }

                foreach (var entry in connectionEntriesToReplace)
                {
                    parentSymbol.RemoveConnection(entry.Connection, entry.MultiInputIndex);
                }

                connectionEntriesToReplace.Reverse(); // restore original order

                var symbolChild = parentSymbol.Children.Single(child => child.Id == instance.SymbolChildId);

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

                newInstanceSymbolChildren.Add((symbolChild, parent, connectionEntriesToReplace));
            }

            // now remove the old instances itself...
            foreach (var instance in InstancesOfSymbol)
            {
                instance.Parent.Children.Remove(instance);
                instance.Dispose();
            }
            InstancesOfSymbol.Clear();

            // ... and create the new ones...
            foreach (var (symbolChild, parent, _) in newInstanceSymbolChildren)
            {
                CreateAndAddNewChildInstance(symbolChild, parent);
            }

            // ... and add the connections again
            newInstanceSymbolChildren.Reverse(); // process reverse that multi input index are correct
            foreach (var (_, parent, connectionsToReplace) in newInstanceSymbolChildren)
            {
                foreach (var entry in connectionsToReplace)
                {
                    parent.Symbol.AddConnection(entry.Connection, entry.MultiInputIndex);
                }
            }
        }

        private static InputDefinition CreateInputDefinition(Guid id, string name, bool isMultiInput, Type valueType)
        {
            // create new input definition
            InputValue defaultValue = InputValueCreators.Entries[valueType]();
            return new InputDefinition { Id = id, Name = name, DefaultValue = defaultValue, IsMultiInput = isMultiInput };
        }

        public Instance CreateInstance(Guid id)
        {
            var newInstance = Activator.CreateInstance(InstanceType) as Instance;
            Debug.Assert(newInstance != null);
            newInstance.SymbolChildId = id;
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
            var conHashToCount = new Dictionary<ulong, int>(Connections.Count);
            foreach (var connection in Connections)
            {
                ulong highPart = 0xFFFFFFFF & (ulong)connection.TargetSlotId.GetHashCode();
                ulong lowPart = 0xFFFFFFFF & (ulong)connection.TargetParentOrChildId.GetHashCode();
                ulong hash = (highPart << 32) | lowPart;
                if (!conHashToCount.TryGetValue(hash, out int count))
                    conHashToCount.Add(hash, 0);

                newInstance.AddConnection(connection, count);

                conHashToCount[hash] = count + 1;
            }

            // connect animations if available
            Animator.CreateUpdateActionsForExistingCurves(newInstance);

            InstancesOfSymbol.Add(newInstance);

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

        private static void RemoveChildInstance(SymbolChild childToRemove, Instance parentInstance)
        {
            var childInstanceToRemove = parentInstance.Children.Single(child => child.SymbolChildId == childToRemove.Id);
            parentInstance.Children.Remove(childInstanceToRemove);
        }

        public void AddConnection(Connection connection, int multiInputIndex = 0)
        {
            var childInputTarget = (from child in Children
                                    where child.Id == connection.TargetParentOrChildId
                                    where child.InputValues.ContainsKey(connection.TargetSlotId)
                                    select child.InputValues[connection.TargetSlotId]).SingleOrDefault();
            bool isMultiInput = childInputTarget?.InputDefinition.IsMultiInput ?? false;

            // check if another connection is already existing to the target input, ignoring multi inputs for now
            var existingConnections = Connections.FindAll(c => c.TargetParentOrChildId == connection.TargetParentOrChildId &&
                                                               c.TargetSlotId == connection.TargetSlotId);
            if (isMultiInput)
            {
                if (multiInputIndex == existingConnections.Count)
                {
                    if (multiInputIndex == 0)
                    {
                        Connections.Add(connection);
                        Log.Info($"Added MI with index {multiInputIndex} at existing index {Connections.Count - 1}");
                    }
                    else
                    {
                        var existingConnection = existingConnections[multiInputIndex - 1];
                        int existingAtIndex = Connections.FindIndex(c => c == existingConnection); // == is intended
                        Connections.Insert(existingAtIndex + 1, connection);
                        Log.Info($"Added MI with index {multiInputIndex} at existing index {existingAtIndex}");
                    }
                }
                else
                {
                    // use the target index to find the existing successor among the connections
                    var existingConnection = existingConnections[multiInputIndex];
                    int existingAtIndex = Connections.FindIndex(c => c == existingConnection); // == is intended
                    Connections.Insert(existingAtIndex, connection);
                    Log.Info($"Added MI with index {multiInputIndex} at existing index {existingAtIndex}");
                }
            }
            else
            {
                if (existingConnections.Count > 0)
                {
                    RemoveConnection(existingConnections[0]);
                }

                Connections.Add(connection);
            }

            foreach (var instance in InstancesOfSymbol)
            {
                instance.AddConnection(connection, multiInputIndex);
            }
        }

        public void RemoveConnection(Connection connection, int multiInputIndex = 0)
        {
            var existingConnections = Connections.FindAll(c => c.TargetParentOrChildId == connection.TargetParentOrChildId &&
                                                               c.TargetSlotId == connection.TargetSlotId);
            if (existingConnections.Count == 0 || multiInputIndex >= existingConnections.Count)
            {
                Log.Error($"Trying to remove a connection that doesn't exist.");
                return;
            }

            var existingConnection = existingConnections[multiInputIndex];
            int connectionsIndex = Connections.FindIndex(c => c == existingConnection); // == is intended
            if (connectionsIndex != -1)
            {
                Log.Info($"Remove  MI with index {multiInputIndex} at existing index {connectionsIndex}");
                Connections.RemoveAt(connectionsIndex);
                foreach (var instance in InstancesOfSymbol)
                {
                    instance.RemoveConnection(connection, multiInputIndex);
                }
            }
        }

        public Guid AddChild(Symbol symbol, Guid addedChildId)
        {
            var newChild = new SymbolChild(symbol, addedChildId);
            Children.Add(newChild);

            foreach (var instance in InstancesOfSymbol)
            {
                CreateAndAddNewChildInstance(newChild, instance);
            }

            return newChild.Id;
        }

        public void RemoveChild(Guid childId)
        {
            // first remove all connections to or from the child
            Connections.RemoveAll(c => c.SourceParentOrChildId == childId || c.TargetParentOrChildId == childId);

            var childToRemove = Children.Single(child => child.Id == childId);
            foreach (var instance in InstancesOfSymbol)
            {
                RemoveChildInstance(childToRemove, instance);
            }
            Children.Remove(childToRemove);
        }

        void DeleteInstance(Instance op)
        {
            InstancesOfSymbol.Remove(op);
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

            public Connection(Guid sourceParentOrChildId, Guid sourceSlotId, Guid targetParentOrChildId, Guid targetSlotId)
            {
                SourceParentOrChildId = sourceParentOrChildId;
                SourceSlotId = sourceSlotId;
                TargetParentOrChildId = targetParentOrChildId;
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

            public bool IsSourceOf(Guid sourceParentOrChildId, Guid sourceSlotId)
            {
                return SourceParentOrChildId == sourceParentOrChildId && SourceSlotId == sourceSlotId;
            }

            public bool IsTargetOf(Guid targetParentOrChildId, Guid targetSlotId)
            {
                return TargetParentOrChildId == targetParentOrChildId && TargetSlotId == targetSlotId;
            }

            public bool IsConnectedToSymbolOutput => TargetParentOrChildId == Guid.Empty;
            public bool IsConnectedToSymbolInput => SourceParentOrChildId == Guid.Empty;
        }
        #endregion
    }
}