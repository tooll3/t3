using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Model;

namespace T3.Core.Operator;

public sealed partial class Symbol
{
    internal void UpdateInstanceType(Instance[] transferredInstances)
    {
        foreach (var instance in transferredInstances)
        {
            _instancesOfSelf.Add(instance);
        }

        UpdateSlotsAndConnectionsForType(out var oldInputDefinitions, out var oldOutputDefinitions);

        var existingInstancesRefreshInfo = new List<InstanceTypeRefreshInfo>();

        var slotChanges = new SlotChangeInfo(oldInputDefinitions, oldOutputDefinitions, InputDefinitions, OutputDefinitions);

        var instances = _instancesOfSelf.ToArray(); // copy since we will modify it within a loop
        // first remove relevant connections from instances and update symbol child input values if needed
        foreach (var instance in instances)
        {
            if (TryUpdateTypeOf(instance, slotChanges, out var refreshInfo))
                existingInstancesRefreshInfo.Add(refreshInfo);
        }

        // now remove the old instances itself...
        foreach (var instance in instances)
        {
            Instance.Destroy(instance);
            _instancesOfSelf.Remove(instance);
        }

        // ... and create the new ones...
        foreach (var (symbolChild, parent, _) in existingInstancesRefreshInfo)
        {
            var success = TryCreateNewInstance(symbolChild, parent, out _);
        }

        // ... and add the connections again
        existingInstancesRefreshInfo.Reverse(); // process reverse that multi input index are correct
        foreach (var (_, parent, connectionsToReplace) in existingInstancesRefreshInfo)
        {
            foreach (var entry in connectionsToReplace)
            {
                parent.Symbol.AddConnection(entry.Connection, entry.MultiInputIndex);
            }
        }

        return;

        void UpdateSlotsAndConnectionsForType(out List<InputDefinition> previousInputDefinitions, out List<OutputDefinition> previousOutputDefinitions)
        {
            var operatorInfo = SymbolPackage.AssemblyInformation.OperatorTypeInfo[Id];

            // check if inputs have changed
            var inputs = operatorInfo.Inputs;

            // todo: it's probably better to first check if there's a change and only then allocate
            previousInputDefinitions = new List<InputDefinition>(InputDefinitions);
            InputDefinitions.Clear();
            foreach (var info in inputs)
            {
                var id = info.Attribute.Id;
                var alreadyExistingInput = previousInputDefinitions.FirstOrDefault(i => i.Id == id);
                if (alreadyExistingInput != null)
                {
                    alreadyExistingInput.Name = info.Name;
                    alreadyExistingInput.IsMultiInput = info.IsMultiInput;
                    InputDefinitions.Add(alreadyExistingInput);
                    previousInputDefinitions.Remove(alreadyExistingInput);
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
            previousOutputDefinitions = new List<OutputDefinition>(OutputDefinitions);
            OutputDefinitions.Clear();
            foreach (var info in outputs)
            {
                var attribute = info.Attribute;
                var alreadyExistingOutput = previousOutputDefinitions.FirstOrDefault(o => o.Id == attribute.Id);
                if (alreadyExistingOutput != null)
                {
                    OutputDefinitions.Add(alreadyExistingOutput);
                    previousOutputDefinitions.Remove(alreadyExistingOutput);
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

                foreach (var input in previousInputDefinitions)
                {
                    if (sourceSlotId == input.Id)
                        connectionsToRemoveWithinSymbol.Add(con);
                }

                foreach (var output in previousOutputDefinitions)
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

            return;

            static InputDefinition CreateInputDefinition(Guid id, string name, bool isMultiInput, Type valueType)
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
        }

        static bool TryUpdateTypeOf(Instance instance, SlotChangeInfo slotChangeInfo, out InstanceTypeRefreshInfo refreshInfo)
        {
            var parent = instance.Parent;
            if (parent == null)
            {
                Log.Error($"Warning: Instance {instance} has no parent. Skipping.");
                refreshInfo = default;
                return false;
            }

            if (!parent.Children.ContainsKey(instance.SymbolChildId))
            {
                // This happens when recompiling ops...
                Log.Error($"Warning: Skipping no longer valid instance of {instance.Symbol} in {parent.Symbol}");
                refreshInfo = default;
                return false;
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
                                                 return slotChangeInfo.OldOutputDefinitions.FirstOrDefault(output =>
                                                        {
                                                            var outputId = output.Id;
                                                            return outputId == c.SourceSlotId ||
                                                                   outputId == c.TargetSlotId;
                                                        }) != null
                                                        || slotChangeInfo.OldInputDefinitions.FirstOrDefault(input =>
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
            foreach (var inputDefinition in slotChangeInfo.NewInputDefinitions)
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
            foreach (var outputDefinition in slotChangeInfo.NewOutputDefinitions)
            {
                var id = outputDefinition.Id;
                if (!oldChildOutputs.TryGetValue(id, out var output))
                {
                    OutputDefinition.TryGetNewValueType(outputDefinition, out var outputData);
                    output = new SymbolChild.Output(outputDefinition, outputData);
                }

                childOutputDict.Add(id, output);
            }

            refreshInfo = new InstanceTypeRefreshInfo(symbolChild, parent, connectionEntriesToReplace);
            return true;
        }
    }

    private class ConnectionEntry
    {
        public Connection Connection { get; set; }
        public int MultiInputIndex { get; set; }
    }

    private readonly record struct InstanceTypeRefreshInfo(SymbolChild SymbolChild, Instance Parent, List<ConnectionEntry> ConnectionsToReplace);

    private readonly record struct SlotChangeInfo(
        IReadOnlyList<InputDefinition> OldInputDefinitions,
        IReadOnlyList<OutputDefinition> OldOutputDefinitions,
        IReadOnlyList<InputDefinition> NewInputDefinitions,
        IReadOnlyList<OutputDefinition> NewOutputDefinitions);
}