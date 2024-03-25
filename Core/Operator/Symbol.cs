using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using T3.Core.Compilation;
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
    public sealed class Symbol : IDisposable
    {
        public Guid Id { get; }
        public string Name { get; private set; }
        public string Namespace { get; private set; }
        public SymbolPackage SymbolPackage { get; set; }

        public readonly List<Instance> InstancesOfSymbol = new();
        public IReadOnlyDictionary<Guid, SymbolChild> Children => _children;
        private readonly Dictionary<Guid, SymbolChild> _children = new();
        public List<Connection> Connections { get; init; } = new();

        /// <summary>
        /// Inputs of this symbol. input values are the default values (exist only once per symbol)
        /// </summary>
        public readonly List<InputDefinition> InputDefinitions = new();

        public readonly List<OutputDefinition> OutputDefinitions = new();
        
        private Type _instanceType;
        public Type InstanceType
        {
            get => _instanceType;
            private set
            {
                _instanceType = value;
                Name = value.Name;
                Namespace = value.Namespace ?? SymbolPackage.AssemblyInformation.Name;
            }
        }

        public Animator Animator { get; } = new();

        public PlaybackSettings PlaybackSettings { get; set; } = new();

        #region public API =======================================================================
        internal void SetChildren(IReadOnlyCollection<SymbolChild> children, bool setChildrensParent)
        {
            foreach (var child in children)
            {
                _children.Add(child.Id, child);
                
                if(setChildrensParent)
                    child.Parent = this;
            }
        }

        public void ForEachSymbolChildInstanceWithId(Guid id, Action<Instance> handler)
        {
            foreach (var symbolInstance in InstancesOfSymbol)
            {
                if(symbolInstance.Children.TryGetValue(id, out var childInstance))
                    handler(childInstance);
            }
        }

        public Symbol(Type instanceType, Guid symbolId, SymbolPackage symbolPackage, Guid[] orderedInputIds = null)
        {
            Id = symbolId;

            UpdateType(instanceType, symbolPackage, out var isObject);

            if (isObject)
                return;

            if (symbolPackage != null)
            {
                UpdateInstanceType();
            }
        }

        public void UpdateType(Type instanceType, SymbolPackage symbolPackage, out bool isObject)
        {
            SymbolPackage = symbolPackage;
            InstanceType = instanceType;
            
            SymbolRegistry.SymbolsByType[instanceType] = this;

            isObject = instanceType == typeof(object);
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

        public void UpdateInstanceType()
        {
            var newInstanceSymbolChildren = new List<(SymbolChild, Instance, List<ConnectionEntry>)>();

            var operatorInfo = SymbolPackage.AssemblyInformation.OperatorTypeInfo[Id];

            // check if inputs have changed
            var inputs = operatorInfo.Inputs;

            // todo: it's probably better to first check if there's a change and only then allocate
            var oldInputDefinitions = new List<InputDefinition>(InputDefinitions);
            InputDefinitions.Clear();
            foreach (var info in inputs)
            {
                var id = info.Attribute.Id;
                var alreadyExistingInput = oldInputDefinitions.FirstOrDefault(i => i.Id == id);
                if (alreadyExistingInput != null)
                {
                    alreadyExistingInput.Name = info.Name;
                    alreadyExistingInput.IsMultiInput = info.IsMultiInput;
                    InputDefinitions.Add(alreadyExistingInput);
                    oldInputDefinitions.Remove(alreadyExistingInput);
                }
                else
                {
                    var isMultiInput = info.IsMultiInput;
                    var valueType = info.GenericArguments[0];
                    var inputDef = CreateInputDefinition(id, info.Name, isMultiInput, valueType);
                    InputDefinitions.Add(inputDef);
                }
            }

            // check if outputs have changed
            var outputs = operatorInfo.Outputs;
            var oldOutputDefinitions = new List<OutputDefinition>(OutputDefinitions);
            OutputDefinitions.Clear();
            foreach (var info in outputs)
            {
                var attribute = info.Attribute;
                var alreadyExistingOutput = oldOutputDefinitions.FirstOrDefault(o => o.Id == attribute.Id);
                if (alreadyExistingOutput != null)
                {
                    OutputDefinitions.Add(alreadyExistingOutput);
                    oldOutputDefinitions.Remove(alreadyExistingOutput);
                }
                else
                {
                    OutputDefinitions.Add(new OutputDefinition
                                              {
                                                  Id = attribute.Id,
                                                  Name = info.Name,
                                                  ValueType = info.GenericArguments[0],
                                                  OutputDataType = info.OutputDataType,
                                                  DirtyFlagTrigger = attribute.DirtyFlagTrigger
                                              });
                }
            }

            var connectionsToRemoveWithinSymbol = new List<Connection>();
            foreach (var con in Connections)
            {
                var sourceSlotId = con.SourceSlotId;
                var targetSlotId = con.TargetSlotId;
                
                foreach (var input in oldInputDefinitions)
                {
                    if (sourceSlotId == input.Id)
                        connectionsToRemoveWithinSymbol.Add(con);
                }

                foreach (var output in oldOutputDefinitions)
                {
                    if (targetSlotId == output.Id)
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
                if (parent == null)
                {
                    Log.Error($"Warning: Instance {instance} has no parent. Skipping.");
                    continue;
                }

                if (!parent.Children.ContainsKey(instance.SymbolChildId))
                {
                    // This happens when recompiling ops...
                    Log.Error($"Warning: Skipping no longer valid instance of {instance.Symbol} in {parent.Symbol}");
                    continue;
                }
                
                var parentSymbol = parent.Symbol;
                var parentConnections = parentSymbol.Connections;
                // get all connections that belong to this instance
                var connectionsToReplace = parentConnections.FindAll(c => c.SourceParentOrChildId == instance.SymbolChildId ||
                                                                                 c.TargetParentOrChildId == instance.SymbolChildId);
                // first remove those connections where the inputs/outputs doesn't exist anymore
                var connectionsToRemove =
                    connectionsToReplace.FindAll(c =>
                                                 {
                                                     return oldOutputDefinitions.FirstOrDefault(output =>
                                                                                                {
                                                                                                    var outputId = output.Id;
                                                                                                    return outputId == c.SourceSlotId ||
                                                                                                        outputId == c.TargetSlotId;
                                                                                                }) != null
                                                            || oldInputDefinitions.FirstOrDefault(input =>
                                                                                                  {
                                                                                                      var inputId = input.Id;
                                                                                                      return inputId == c.SourceSlotId ||
                                                                                                          inputId == c.TargetSlotId;
                                                                                                  }) != null;
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
                                        MultiInputIndex = parentConnections.FindAll(c => c.TargetParentOrChildId == con.TargetParentOrChildId
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

                var symbolChild = instance.SymbolChild;

                // update inputs of symbol child
                var childInputDict = symbolChild.Inputs;
                var oldChildInputs = new Dictionary<Guid, SymbolChild.Input>(childInputDict);
                childInputDict.Clear();
                foreach (var inputDefinition in InputDefinitions)
                {
                    var inputId = inputDefinition.Id;
                    var inputToAdd = oldChildInputs.TryGetValue(inputId, out var oldInput)
                                         ? oldInput
                                         : new SymbolChild.Input(inputDefinition);

                    childInputDict.Add(inputId, inputToAdd);
                }

                // update output of symbol child
                var childOutputDict = symbolChild.Outputs;
                var oldChildOutputs = new Dictionary<Guid, SymbolChild.Output>(childOutputDict);
                childOutputDict.Clear();
                foreach (var outputDefinition in OutputDefinitions)
                {
                    var id = outputDefinition.Id;
                    if (!oldChildOutputs.TryGetValue(id, out var output))
                    {
                        OutputDefinition.TryGetNewValueType(outputDefinition, out var outputData);
                        output = new SymbolChild.Output(outputDefinition, outputData);
                    }

                    childOutputDict.Add(id, output);
                }

                newInstanceSymbolChildren.Add((symbolChild, parent, connectionEntriesToReplace));
            }

            var instanceList = InstancesOfSymbol;
            var maxInstanceIndex = instanceList.Count - 1;
            // now remove the old instances itself...
            for (var index = maxInstanceIndex; index >= 0; index--)
            {
                var instance = instanceList[index];
                instance.Parent?.Children.Remove(instance.SymbolChildId);
                instance.Dispose();
                instanceList.RemoveAt(index);
            }

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
            if (!InputValueCreators.Entries.TryGetValue(valueType, out var creationFunc))
            {
                Log.Error("Can't create default value for " + valueType);
                return null;
            }

            try
            {
                return new InputDefinition { Id = id, Name = name, DefaultValue = creationFunc(), IsMultiInput = isMultiInput };
            }
            catch (Exception e)
            {
                Log.Error($"Failed to create default value for {valueType}: {e}");
                return null;
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
            var lastIndex = numInputs - 1;

            var inputDefinitions = InputDefinitions;
            for (int i = 0; i < lastIndex; i++)
            {
                Guid inputId = inputDefinitions[i].Id;
                if (inputs[i].Id != inputId)
                {
                    int index = inputs.FindIndex(i + 1, input => input.Id == inputId);
                    Debug.Assert(index >= 0);
                    inputs.Swap(i, index);
                    Debug.Assert(inputId == inputs[i].Id);
                }
            }

            #if DEBUG
            if (numInputs > 0)
            {
                Debug.Assert(inputs.Count == InputDefinitions.Count);
            }
            #endif
        }

        public Instance CreateInstance(Instance parent, SymbolChild symbolChild)
        {
            Instance newInstance;
            if (SymbolPackage.AssemblyInformation.OperatorTypeInfo.TryGetValue(Id, out var typeInfo))
            {
                var constructor = typeInfo.Constructor;
                try
                {
                    newInstance = (Instance)constructor.Invoke();
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create instance of type {InstanceType} with id {symbolChild.Id}: {e}");
                    return null;
                }
            }
            else
            {
                Log.Error($"No constructor found for {InstanceType}. This is likely an old operator version - it is recommended you add it");

                try
                {
                    // create instance through reflection
                    newInstance = (Instance)Activator.CreateInstance(InstanceType, AssemblyInformation.ConstructorBindingFlags, 
                                                                     binder: null, args: Array.Empty<object>(), culture: null);
                }
                catch (Exception e)
                {
                    Log.Error($"(Instance creation fallback failure) Failed to create instance of type {InstanceType} with id {symbolChild.Id}: {e}");
                    return null;
                }
            }

            Debug.Assert(newInstance != null);
            newInstance!.SymbolChild = symbolChild;
            
            newInstance.Parent = parent;

            SortInputSlotsByDefinitionOrder(newInstance.Inputs);
            InstancesOfSymbol.Add(newInstance);

            // adds children to the new instance
            foreach (var child in _children.Values)
            {
                CreateAndAddNewChildInstance(child, newInstance);
            }

            // create connections between instances
            var connections = Connections;
            if (connections.Count != 0)
            {
                var conHashToCount = new Dictionary<ulong, int>(connections.Count);
                for (var index = 0; index < connections.Count; index++) // warning: the order in which these are processed matters
                {
                    var connection = connections[index];
                    ulong highPart = 0xFFFFFFFF & (ulong)connection.TargetSlotId.GetHashCode();
                    ulong lowPart = 0xFFFFFFFF & (ulong)connection.TargetParentOrChildId.GetHashCode();
                    ulong hash = (highPart << 32) | lowPart;
                    if (!conHashToCount.TryGetValue(hash, out int count))
                        conHashToCount.Add(hash, 0);

                    var valid = newInstance.TryAddConnection(connection, count);
                    if (!valid)
                    {
                        Log.Warning($"Removing obsolete connecting in {this}...");
                        connections.RemoveAt(index);
                        index--;
                        continue;
                    }

                    conHashToCount[hash] = count + 1;
                }
            }

            // connect animations if available
            Animator.CreateUpdateActionsForExistingCurves(newInstance.Children.Values);

            return newInstance;
        }

        private static Instance CreateAndAddNewChildInstance(SymbolChild symbolChild, Instance parentInstance)
        {
            // cache property accesses for performance
            var childSymbol = symbolChild.Symbol;
            var childInstance = childSymbol.CreateInstance(parentInstance, symbolChild);

            var childInputDefinitions = childSymbol.InputDefinitions;
            var childInputDefinitionCount = childInputDefinitions.Count;
            
            var childInputs = childInstance.Inputs;
            var childInputCount = childInputs.Count;
            
            var symbolChildInputs = symbolChild.Inputs;
            
            // set up the inputs for the child instance
            for (int i = 0; i < childInputDefinitionCount; i++)
            {
                if (i >= childInputCount)
                {
                    Log.Warning($"Skipping undefined input index");
                    continue;
                }
                
                var inputDefinitionId = childInputDefinitions[i].Id;
                var inputSlot = childInputs[i];
                if (!symbolChildInputs.TryGetValue(inputDefinitionId, out var input))
                {
                    Log.Warning($"Skipping undefined input: {inputDefinitionId}");
                    continue;
                }
                inputSlot.Input = input;
                inputSlot.Id = inputDefinitionId;
            }
            
            // cache property accesses for performance
            var childOutputDefinitions = childSymbol.OutputDefinitions;
            var childOutputDefinitionCount = childOutputDefinitions.Count;
            
            var childOutputs = childInstance.Outputs;
            
            var symbolChildOutputs = symbolChild.Outputs;

            // set up the outputs for the child instance
            for (int i = 0; i < childOutputDefinitionCount; i++)
            {
                Debug.Assert(i < childOutputs.Count);
                var outputSlot = childOutputs[i];
                var outputDefinition = childOutputDefinitions[i];
                var id = outputDefinition.Id;
                outputSlot.Id = id;
                var symbolChildOutput = symbolChildOutputs[id];
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

            parentInstance.Children.Add(childInstance.SymbolChildId, childInstance);

            if (symbolChild.IsBypassed)
            {
                symbolChild.IsBypassed = true;
            }

            return childInstance;
        }

        public bool IsTargetMultiInput(Connection connection)
        {
            return Children.TryGetValue(connection.TargetParentOrChildId, out var child) 
                   && child.Inputs.TryGetValue(connection.TargetSlotId, out var targetSlot) 
                   && targetSlot.IsMultiInput;
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
            var targetParentOrChildId = connection.TargetParentOrChildId;
            var targetSlotId = connection.TargetSlotId;

            List<Connection> connectionsAtInput = new();
            
            var connections = Connections;

            foreach (var potentialConnection in connections)
            {
                if (potentialConnection.TargetParentOrChildId == targetParentOrChildId &&
                    potentialConnection.TargetSlotId == targetSlotId)
                {
                    connectionsAtInput.Add(potentialConnection);
                }
            }

            var connectionsAtInputCount = connectionsAtInput.Count;
            if (connectionsAtInputCount == 0 || multiInputIndex >= connectionsAtInputCount)
            {
                Log.Error($"Trying to remove a connection that doesn't exist. Index {multiInputIndex} of {connectionsAtInput.Count}");
                return;
            }

            var existingConnection = connectionsAtInput[multiInputIndex];

            // ReSharper disable once PossibleUnintendedReferenceComparison
            bool removed = false;
            var connectionCount = connections.Count;
            for (var index = 0; index < connectionCount; index++)
            {
                if (connections[index] == existingConnection)
                {
                    connections.RemoveAt(index);
                    removed = true;
                    break;
                }
            }

            if (removed)
            {
                foreach (var instance in InstancesOfSymbol)
                {
                    instance.RemoveConnection(connection, multiInputIndex);
                }
            }
            else
            {
                Log.Warning($"Failed to remove connection.");
            }
        }

        public SymbolChild AddChild(Symbol symbol, Guid addedChildId, string name = null)
        {
            var newChild = new SymbolChild(symbol, addedChildId, this)
                               {
                                   Name = name
                               };
            
            _children.Add(newChild.Id, newChild);

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
                Animator.CreateUpdateActionsForExistingCurves(symbolInstance.Children.Values);
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

        public bool RemoveChild(Guid childId, out bool removedFromAll)
        {
            // first remove all connections to or from the child
            Connections.RemoveAll(c => c.SourceParentOrChildId == childId || c.TargetParentOrChildId == childId);

            removedFromAll = true;
            foreach (var instance in InstancesOfSymbol)
            {
                removedFromAll &= instance.Children.Remove(childId);
            }

            var removedFromSymbol = _children.Remove(childId);
            removedFromAll &= removedFromSymbol;
            return removedFromSymbol;
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
        #endregion

        #region sub classses =============================================================================
        /// <summary>
        /// Options on the visual presentation of <see cref="Symbol"/> input.
        /// </summary>
        public sealed class InputDefinition
        {
            public Guid Id { get; internal init; }
            public string Name { get; internal set; }
            public InputValue DefaultValue { get; set; }
            public bool IsMultiInput { get; internal set; }
        }

        public sealed class OutputDefinition
        {
            public Guid Id { get; init; }
            public string Name { get; init; }
            public Type ValueType { get; init; }
            public Type OutputDataType { get; init; }
            public DirtyFlagTrigger DirtyFlagTrigger { get; init; }

            private static readonly ConcurrentDictionary<Type, Func<object>> OutputValueConstructors = new();

            public static bool TryGetNewValueType(OutputDefinition def, out IOutputData newData)
            {
                return TryCreateOutputType(def, out newData, def.ValueType);
            }
            
            public static bool TryGetNewOutputDataType(OutputDefinition def, out IOutputData newData)
            {
                return TryCreateOutputType(def, out newData, def.OutputDataType);
            }

            private static bool TryCreateOutputType(OutputDefinition def, out IOutputData newData, Type valueType)
            {
                if (valueType == null)
                {
                    newData = null;
                    return false;
                }
                
                if (OutputValueConstructors.TryGetValue(valueType, out var constructor))
                {
                    newData = (IOutputData)constructor();
                    return true;
                }

                if (!valueType.IsAssignableTo(typeof(IOutputData)))
                {
                    Log.Warning($"Value type {valueType} for output {def.Name} is not an {nameof(IOutputData)}");
                    newData = null;
                    return false;
                }

                constructor = Expression.Lambda<Func<object>>(Expression.New(valueType)).Compile();
                OutputValueConstructors[valueType] = constructor;
                newData = (IOutputData)constructor();
                return true;
            }
        }

        public class Connection
        {
            public Guid SourceParentOrChildId { get; }
            public Guid SourceSlotId { get; }
            public Guid TargetParentOrChildId { get; }
            public Guid TargetSlotId { get; }

            private readonly int _hashCode;

            public Connection(in Guid sourceParentOrChildId, in Guid sourceSlotId, in Guid targetParentOrChildId, in Guid targetSlotId)
            {
                SourceParentOrChildId = sourceParentOrChildId;
                SourceSlotId = sourceSlotId;
                TargetParentOrChildId = targetParentOrChildId;
                TargetSlotId = targetSlotId;
                
                // pre-compute hash code as this is read-only
                _hashCode = CalculateHashCode(sourceParentOrChildId, sourceSlotId, targetParentOrChildId, targetSlotId);
            }

            public sealed override int GetHashCode() => _hashCode;

            private int CalculateHashCode(in Guid sourceParentOrChildId, in Guid sourceSlotId, in Guid targetParentOrChildId, in Guid targetSlotId)
            {
                int hash = sourceParentOrChildId.GetHashCode();
                hash = hash * 31 + sourceSlotId.GetHashCode();
                hash = hash * 31 + targetParentOrChildId.GetHashCode();
                hash = hash * 31 + targetSlotId.GetHashCode();
                return hash;
            }

            public sealed override bool Equals(object other)
            {
                return GetHashCode() == other?.GetHashCode();
            }
            
            public static bool operator ==(Connection a, Connection b) => a?.GetHashCode() == b?.GetHashCode();
            public static bool operator !=(Connection a, Connection b) => a?.GetHashCode() != b?.GetHashCode();
            

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
                var childInstance = symbolInstance.Children[childId];
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