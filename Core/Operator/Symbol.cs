using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

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
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string PendingSource { get; set; }
        public string DeprecatedSourcePath { get; set; }

        public readonly List<Instance> InstancesOfSymbol = new();
        public readonly List<SymbolChild> Children = new();
        public readonly List<Connection> Connections = new();

        /// <summary>
        /// Inputs of this symbol. input values are the default values (exist only once per symbol)
        /// </summary>
        public readonly List<InputDefinition> InputDefinitions = new();

        public readonly List<OutputDefinition> OutputDefinitions = new();

        public Type InstanceType { get; private set; }

        public Animator Animator { get; } = new();

        public PlaybackSettings PlaybackSettings { get; set; } = new();

        #region public API =======================================================================
        internal void SetChildren (IReadOnlyCollection<SymbolChild> children, bool setChildrensParent)
        {
            Children.AddRange(children);

            if (!setChildrensParent)
                return;

            foreach (var child in Children)
            {
                child.Parent = this;
            }
        }

        public void ForEachSymbolChildInstanceWithId(Guid id, Action<Instance> handler)
        {
            var matchingInstances = from symbolInstance in InstancesOfSymbol
                                    from childInstance in symbolInstance.Children
                                    where childInstance.SymbolChildId == id
                                    select childInstance;

            foreach (var childInstance in matchingInstances)
            {
                handler(childInstance);
            }
        }

        public Symbol(Type instanceType, Guid symbolId, Guid[] orderedInputIds = null)
        {
            InstanceType = instanceType;
            Name = instanceType.Name;
            Id = symbolId;

            // input identified by base interface
            Type inputSlotType = typeof(IInputSlot);


            var inputInfos = instanceType.GetFields().Where(f => inputSlotType.IsAssignableFrom(f.FieldType));
            var inputDefs = new List<InputDefinition>();
            foreach (var inputInfo in inputInfos)
            {
                var customAttributes = inputInfo.GetCustomAttributes(typeof(InputAttribute), false);
                Debug.Assert(customAttributes.Length == 1);
                var attribute = (InputAttribute)customAttributes[0];
                var isMultiInput = inputInfo.FieldType.GetGenericTypeDefinition() == typeof(MultiInputSlot<>);
                var valueType = inputInfo.FieldType.GetGenericArguments()[0];
                
                if (!TypeNameRegistry.Entries.ContainsKey(valueType))
                {
                    Log.Error($"Skipping input {Name}.{inputInfo.Name} with undefined type {valueType}...");
                    continue;
                }
                var inputDef = CreateInputDefinition(attribute.Id, inputInfo.Name, isMultiInput, valueType);
                inputDefs.Add(inputDef);
            }

            // add in order for input ids that are given
            if (orderedInputIds != null)
            {
                foreach (Guid id in orderedInputIds)
                {
                    var inputDefinition = inputDefs.Find(inputDef => inputDef != null && inputDef.Id == id);
                    
                    if (inputDefinition != null)
                    {
                        InputDefinitions.Add(inputDefinition);
                        inputDefs.Remove(inputDefinition);
                    }
                }
            }

            // add the ones where no id was available to the end
            InputDefinitions.AddRange(inputDefs);

            // outputs identified by attribute
            var outputs = (from field in instanceType.GetFields()
                           let attributes = field.GetCustomAttributes(typeof(OutputAttribute), false)
                           from attr in attributes
                           select (field, attributes)).ToArray();
            foreach (var (output, attributes) in outputs)
            {
                var valueType = output.FieldType.GenericTypeArguments[0];
                var attribute = (OutputAttribute)attributes.First();
                var outputDataType = GetOutputDataType(output);
                
                if (!TypeNameRegistry.Entries.ContainsKey(valueType))
                {
                    Log.Error($"Skipping output {Name}.{output.Name} with undefined type {valueType}...");
                    continue;
                }

                OutputDefinitions.Add(new OutputDefinition
                                          {
                                              Id = attribute.Id,
                                              Name = output.Name,
                                              ValueType = valueType,
                                              OutputDataType = outputDataType,
                                              DirtyFlagTrigger = attribute.DirtyFlagTrigger
                                          });
            }
        }

        private static Type GetOutputDataType(FieldInfo output)
        {
            Type outputDataInterfaceType = (from interfaceType in output.FieldType.GetInterfaces()
                                            where interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IOutputDataUser<>)
                                            select interfaceType).SingleOrDefault();
            return outputDataInterfaceType?.GetGenericArguments().Single();
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

        public int GetMultiInputIndexFor(Connection con)
        {
            return Connections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
                                            && c.TargetSlotId == con.TargetSlotId)
                              .FindIndex(cc => cc == con); // todo: fix this mess! connection rework!
        }

        public void UpdateInstanceType(Type instanceType)
        {
            InstanceType = instanceType;
            Name = instanceType.Name;
            Log.Info($"New instance type name: {Name}");
            var newInstanceSymbolChildren = new List<(SymbolChild, Instance, List<ConnectionEntry>)>();

            // check if inputs have changed
            Type inputSlotType = typeof(IInputSlot);
            var inputInfos = instanceType.GetFields().Where(f => inputSlotType.IsAssignableFrom(f.FieldType));

            (FieldInfo inputInfo, InputAttribute)[] inputs = null;

            try
            {
                inputs = (from inputInfo in inputInfos
                          let customAttributes = inputInfo.GetCustomAttributes(typeof(InputAttribute), false)
                          where customAttributes.Any()
                          select (inputInfo, (InputAttribute)customAttributes[0])).ToArray();
            }
            catch (Exception e)
            {
                Log.Error($"Failed get input attribute type:{e.Message}\n {e.InnerException}");
            }

            if (inputs == null)
                return;

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
                    var outputDataType = GetOutputDataType(output);
                    OutputDefinitions.Add(new OutputDefinition
                                              {
                                                  Id = attribute.Id,
                                                  Name = output.Name,
                                                  ValueType = valueType,
                                                  OutputDataType = outputDataType,
                                                  DirtyFlagTrigger = attribute.DirtyFlagTrigger
                                              });
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
                //
                if (parent == null)
                {
                    Log.Error($"Warning: Skipping instance without parent {instance.Symbol}");
                    continue;
                }

                if (parent == null || !parent.Children.Contains(instance))
                {
                    // This happens when recompiling ops...
                    //Log.Error($"Warning: Skipping no longer valid instance of {instance.Symbol} in {parent.Symbol}");
                    continue;
                }

                var parentSymbol = parent.Symbol;
                // get all connections that belong to this instance
                var connectionsToReplace = parentSymbol.Connections.FindAll(c => c.SourceParentOrChildId == instance.SymbolChildId ||
                                                                                 c.TargetParentOrChildId == instance.SymbolChildId);
                // first remove those connections where the inputs/outputs doesn't exist anymore
                var connectionsToRemove =
                    connectionsToReplace.FindAll(c =>
                                                 {
                                                     return oldOutputDefinitions.FirstOrDefault(output => output.Id == c.SourceSlotId ||
                                                                                                    output.Id == c.TargetSlotId) != null ||
                                                            oldInputDefinitions.FirstOrDefault(input => input.Id == c.SourceSlotId ||
                                                                                                        input.Id == c.TargetSlotId) != null;
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
                var oldChildInputs = new Dictionary<Guid, SymbolChild.Input>(symbolChild.Inputs);
                symbolChild.Inputs.Clear();
                foreach (var inputDefinition in InputDefinitions)
                {
                    if (oldChildInputs.TryGetValue(inputDefinition.Id, out var input))
                    {
                        symbolChild.Inputs.Add(inputDefinition.Id, input);
                    }
                    else
                    {
                        symbolChild.Inputs.Add(inputDefinition.Id, new SymbolChild.Input(inputDefinition));
                    }
                }

                // update output of symbol child
                var oldChildOutputs = new Dictionary<Guid, SymbolChild.Output>(symbolChild.Outputs);
                symbolChild.Outputs.Clear();
                foreach (var outputDefinition in OutputDefinitions)
                {
                    if (oldChildOutputs.TryGetValue(outputDefinition.Id, out var output))
                    {
                        symbolChild.Outputs.Add(outputDefinition.Id, output);
                    }
                    else
                    {
                        var outputData = (outputDefinition.OutputDataType != null)
                                             ? (Activator.CreateInstance(outputDefinition.OutputDataType) as IOutputData)
                                             : null;
                        var newOutput = new SymbolChild.Output(outputDefinition, outputData);
                        symbolChild.Outputs.Add(outputDefinition.Id, newOutput);
                    }
                }

                newInstanceSymbolChildren.Add((symbolChild, parent, connectionEntriesToReplace));
            }

            // now remove the old instances itself...
            foreach (var instance in InstancesOfSymbol)
            {
                instance.Parent?.Children.Remove(instance);
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
            try
            {
                InputValue defaultValue = InputValueCreators.Entries[valueType]();
                return new InputDefinition { Id = id, Name = name, DefaultValue = defaultValue, IsMultiInput = isMultiInput };
            }
            catch
            {
                Log.Error("Can't create default value for " + valueType);
                return null;
            }
        }

        public void SwapInputs(int indexA, int indexB)
        {
            InputDefinitions.Swap(indexA, indexB);

            foreach (var instance in InstancesOfSymbol)
            {
                instance.Inputs.Swap(indexA, indexB);
            }
        }

        public void SortInputSlotsByDefinitionOrder()
        {
            foreach (var instance in InstancesOfSymbol)
            {
                SortInputSlotsByDefinitionOrder(instance.Inputs);
            }
        }

        private void SortInputSlotsByDefinitionOrder(List<IInputSlot> inputs)
        {
            // order the inputs by the given input definitions. original order is coming from code, but input def order is the relevant one
            int numInputs = inputs.Count;
            
            for (int i = 0; i < numInputs - 1; i++)
            {
                Guid inputId = InputDefinitions[i].Id;
                if (inputs[i].Id != inputId)
                {
                    int index = inputs.FindIndex(i + 1, input => input.Id == inputId);
                    Debug.Assert(index >= 0);
                    inputs.Swap(i, index);
                }
            }

            // verify the order
            for (int i = 0; i < numInputs; i++)
            {
                Debug.Assert(InputDefinitions[i].Id == inputs[i].Id);
            }
        }

        public Instance CreateInstance(Guid id)
        {
            var newInstance = Activator.CreateInstance(InstanceType) as Instance;
            Debug.Assert(newInstance != null);
            newInstance.SymbolChildId = id;
            newInstance.Symbol = this;

            SortInputSlotsByDefinitionOrder(newInstance.Inputs);
            InstancesOfSymbol.Add(newInstance);

            foreach (var child in Children)
            {
                CreateAndAddNewChildInstance(child, newInstance);
            }

            // create connections between instances
            if (Connections.Count != 0)
            {
                var conHashToCount = new Dictionary<ulong, int>(Connections.Count);
                for (var index = 0; index < Connections.Count; index++) // warning: the order in which these are processed matters
                {
                    var connection = Connections[index];
                    ulong highPart = 0xFFFFFFFF & (ulong)connection.TargetSlotId.GetHashCode();
                    ulong lowPart = 0xFFFFFFFF & (ulong)connection.TargetParentOrChildId.GetHashCode();
                    ulong hash = (highPart << 32) | lowPart;
                    if (!conHashToCount.TryGetValue(hash, out int count))
                        conHashToCount.Add(hash, 0);

                    var valid = newInstance.TryAddConnection(connection, count);
                    if (!valid)
                    {
                        Log.Warning("Skipping connection to no longer existing targets");
                        Connections.RemoveAt(index);
                        index--;
                        continue;
                    }

                    conHashToCount[hash] = count + 1;
                }
            }

            // connect animations if available
            Animator.CreateUpdateActionsForExistingCurves(newInstance.Children);

            return newInstance;
        }

        private static Instance CreateAndAddNewChildInstance(SymbolChild symbolChild, Instance parentInstance)
        {
            var childSymbol = symbolChild.Symbol;
            var childInstance = childSymbol.CreateInstance(symbolChild.Id);
            childInstance.Parent = parentInstance;

            // set up the inputs for the child instance
            for (int i = 0; i < symbolChild.Symbol.InputDefinitions.Count; i++)
            {
                if (i >= childInstance.Inputs.Count)
                {
                    Log.Warning($"Skipping undefined input index");
                    continue;
                }
                
                var inputDefinitionId = childSymbol.InputDefinitions[i].Id;
                var inputSlot = childInstance.Inputs[i];
                if (!symbolChild.Inputs.TryGetValue(inputDefinitionId, out var input))
                {
                    Log.Warning($"Skipping undefined input: {inputDefinitionId}");
                    continue;
                }
                inputSlot.Input = input;
                inputSlot.Id = inputDefinitionId;
            }

            // set up the outputs for the child instance
            for (int i = 0; i < symbolChild.Symbol.OutputDefinitions.Count; i++)
            {
                Debug.Assert(i < childInstance.Outputs.Count);
                var outputSlot = childInstance.Outputs[i];
                var outputDefinition = childSymbol.OutputDefinitions[i];
                outputSlot.Id = outputDefinition.Id;
                var symbolChildOutput = symbolChild.Outputs[outputSlot.Id];
                if (outputDefinition.OutputDataType != null)
                {
                    // output is using data, so link it
                    if (outputSlot is IOutputDataUser outputDataConsumer)
                    {
                        outputDataConsumer.SetOutputData(symbolChildOutput.OutputData);
                    }
                }

                outputSlot.DirtyFlag.Trigger = symbolChildOutput.DirtyFlagTrigger;
                outputSlot.IsDisabled = symbolChildOutput.IsDisabled;
            }

            parentInstance.Children.Add(childInstance);

            if (symbolChild.IsBypassed)
            {
                symbolChild.IsBypassed = true;
            }

            return childInstance;
        }

        private static void RemoveChildInstance(SymbolChild childToRemove, Instance parentInstance)
        {
            var childInstanceToRemove = parentInstance.Children.Single(child => child.SymbolChildId == childToRemove.Id);
            parentInstance.Children.Remove(childInstanceToRemove);
        }

        public bool IsTargetMultiInput(Connection connection)
        {
            var childInputTarget = (from child in Children
                                    where child.Id == connection.TargetParentOrChildId
                                    where child.Inputs.ContainsKey(connection.TargetSlotId)
                                    select child.Inputs[connection.TargetSlotId]).SingleOrDefault();
            bool isMultiInput = childInputTarget?.InputDefinition.IsMultiInput ?? false;

            return isMultiInput;
        }

        /// <summary>
        /// Add connection to symbol and its instances
        /// </summary>
        /// <remarks>All connections of a symbol are stored in a single List, from which sorting of multi-inputs
        /// is define. That why inserting connections for those requires to first find the correct index within that
        /// larger list. 
        /// </remarks>
        public void AddConnection(Connection connection, int multiInputIndex = 0)
        {
            var isMultiInput = IsTargetMultiInput(connection);

            // Check if another connection is already existing to the target input, ignoring multi inputs for now
            var connectionsAtInput = Connections.FindAll(c =>
                                                             c.TargetParentOrChildId == connection.TargetParentOrChildId &&
                                                             c.TargetSlotId == connection.TargetSlotId);

            if (multiInputIndex > connectionsAtInput.Count)
            {
                Log.Error($"Trying to add a connection at the index {multiInputIndex}. Out of bound of the {connectionsAtInput.Count} existing connections.");
                return;
            }

            if (!isMultiInput)
            {
                // Replace existing on single inputs
                if (connectionsAtInput.Count > 0)
                {
                    RemoveConnection(connectionsAtInput[0]);
                }

                Connections.Add(connection);
            }
            else
            {
                var insertBefore = multiInputIndex < connectionsAtInput.Count;
                if (insertBefore)
                {
                    // Use the target index to find the existing successor among the connections
                    var existingConnection = connectionsAtInput[multiInputIndex];

                    // ReSharper disable once PossibleUnintendedReferenceComparison
                    var insertIndex = Connections.FindIndex(c => c == existingConnection);

                    Connections.Insert(insertIndex, connection);
                }
                else
                {
                    if (connectionsAtInput.Count == 0)
                    {
                        Connections.Add(connection);
                    }
                    else
                    {
                        var existingConnection = connectionsAtInput[^1];

                        // ReSharper disable once PossibleUnintendedReferenceComparison
                        var insertIndex = Connections.FindIndex(c => c == existingConnection);
                        Connections.Insert(insertIndex + 1, connection);
                    }
                }
            }

            foreach (var instance in InstancesOfSymbol)
            {
                instance.TryAddConnection(connection, multiInputIndex);
            }
        }

        public void RemoveConnection(Connection connection, int multiInputIndex = 0)
        {
            var connectionsAtInput = Connections.FindAll(c =>
                                                              c.TargetParentOrChildId == connection.TargetParentOrChildId &&
                                                              c.TargetSlotId == connection.TargetSlotId);
            
            if (connectionsAtInput.Count == 0 || multiInputIndex >= connectionsAtInput.Count)
            {
                Log.Error($"Trying to remove a connection that doesn't exist. Index {multiInputIndex} of {connectionsAtInput.Count}");
                return;
            }

            var existingConnection = connectionsAtInput[multiInputIndex];
            
            // ReSharper disable once PossibleUnintendedReferenceComparison
            var connectionIndex = Connections.FindIndex(c => c == existingConnection); // == is intended
            if (connectionIndex == -1)
                return;
            
            //Log.Info($"Remove  MI with index {multiInputIndex} at existing index {connectionsIndex}");
            Connections.RemoveAt(connectionIndex);
            foreach (var instance in InstancesOfSymbol)
            {
                instance.RemoveConnection(connection, multiInputIndex);
            }
        }

        public SymbolChild AddChild(Symbol symbol, Guid addedChildId, string name = null)
        {
            var newChild = new SymbolChild(symbol, addedChildId, this)
                               {
                                   Name = name
                               };
            Children.Add(newChild);

            var childInstances = new List<Instance>(InstancesOfSymbol.Count);
            foreach (var instance in InstancesOfSymbol)
            {
                var newChildInstance = CreateAndAddNewChildInstance(newChild, instance);
                childInstances.Add(newChildInstance);
            }

            Animator.CreateUpdateActionsForExistingCurves(childInstances);
            return newChild;
        }

        public void CreateOrUpdateActionsForAnimatedChildren()
        {
            foreach (var symbolInstance in InstancesOfSymbol)
            {
                Animator.CreateUpdateActionsForExistingCurves(symbolInstance.Children);
            }
        }

        public void CreateAnimationUpdateActionsForSymbolInstances()
        {
            var parents = new HashSet<Symbol>();
            foreach (var instance in InstancesOfSymbol)
            {
                parents.Add(instance.Parent.Symbol);
            }

            foreach (var parentSymbol in parents)
            {
                parentSymbol.CreateOrUpdateActionsForAnimatedChildren();
            }
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

        public InputDefinition GetInputMatchingType(Type type)
        {
            foreach (var inputDefinition in InputDefinitions)
            {
                if (type == null || inputDefinition.DefaultValue.ValueType == type)
                    return inputDefinition;
            }

            return null;
        }

        public OutputDefinition GetOutputMatchingType(Type type)
        {
            foreach (var outputDefinition in OutputDefinitions)
            {
                if (type == null || outputDefinition.ValueType == type)
                    return outputDefinition;
            }

            return null;
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
            public Type OutputDataType { get; set; }
            public DirtyFlagTrigger DirtyFlagTrigger { get; set; }
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

        public void InvalidateInputInAllChildInstances(IInputSlot inputSlot)
        {
            var childId = inputSlot.Parent.SymbolChildId;
            var inputId = inputSlot.Id;
            InvalidateInputInAllChildInstances(inputId, childId);
        }

        public void InvalidateInputInAllChildInstances(Guid inputId, Guid childId)
        {
            foreach (var symbolInstance in InstancesOfSymbol)
            {
                var childInstance = symbolInstance.Children.Single(c => c.SymbolChildId == childId);
                var slot = childInstance.Inputs.Single(i => i.Id == inputId);
                slot.DirtyFlag.Invalidate();
            }
        }

        
        /// <summary>
        /// Invalidates all instances of a symbol input (e.g. if that input's default was modified)
        /// </summary>
        public void InvalidateInputDefaultInInstances(IInputSlot inputSlot)
        {
            var inputId = inputSlot.Id;
            foreach (var symbolInstance in InstancesOfSymbol)
            {
                var slot = symbolInstance.Inputs.Single(i => i.Id == inputId);
                if (!slot.Input.IsDefault)
                    continue;
                
                slot.DirtyFlag.Invalidate();
            }
        }
    }
}