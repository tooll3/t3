using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Model;
using T3.Core.SystemUi;

namespace T3.Core.Operator;

public sealed partial class Symbol
{
    internal void UpdateInstanceType()
    {
        if (!NeedsTypeUpdate)
            return;
        
        NeedsTypeUpdate = false;
        
        UpdateSlotsAndConnectionsForType(out var oldInputDefinitions, out var oldOutputDefinitions);

        var slotChanges = new SlotChangeInfo(oldInputDefinitions, oldOutputDefinitions, InputDefinitions, OutputDefinitions);

        var count = _childrenCreatedFromMe.Count;
        if (count == 0)
            return;

        foreach (var child in _childrenCreatedFromMe)
        {
            child.UpdateIOAndConnections(slotChanges);
        }

        return;

        void UpdateSlotsAndConnectionsForType(out List<InputDefinition> removedInputDefinitions, out List<OutputDefinition> removedOutputDefinitions)
        {
            var operatorInfo = SymbolPackage.AssemblyInformation.OperatorTypeInfo[Id];

            // check if inputs have changed
            var inputs = operatorInfo.Inputs;

            // todo: it's probably better to first check if there's a change and only then allocate
            removedInputDefinitions = new List<InputDefinition>(InputDefinitions);
            InputDefinitions.Clear();
            foreach (var info in inputs)
            {
                var id = info.Attribute.Id;
                var alreadyExistingInput = removedInputDefinitions.FirstOrDefault(i => i.Id == id);
                if (alreadyExistingInput != null)
                {
                    alreadyExistingInput.Name = info.Name;
                    alreadyExistingInput.IsMultiInput = info.IsMultiInput;
                    InputDefinitions.Add(alreadyExistingInput);
                    removedInputDefinitions.Remove(alreadyExistingInput);
                }
                else
                {
                    var isMultiInput = info.IsMultiInput;
                    var valueType = info.GenericArguments[0];
                    
                    if(TryCreateInputDefinition(id, info.Name, isMultiInput, valueType, _instanceType, out var inputDef))
                        InputDefinitions.Add(inputDef);
                }
            }

            // check if outputs have changed
            var outputs = operatorInfo.Outputs;
            removedOutputDefinitions = new List<OutputDefinition>(OutputDefinitions);
            OutputDefinitions.Clear();
            foreach (var info in outputs)
            {
                var attribute = info.Attribute;
                var alreadyExistingOutput = removedOutputDefinitions.FirstOrDefault(o => o.Id == attribute.Id);
                if (alreadyExistingOutput != null)
                {
                    OutputDefinitions.Add(alreadyExistingOutput);
                    removedOutputDefinitions.Remove(alreadyExistingOutput);
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

                foreach (var input in removedInputDefinitions)
                {
                    if (sourceSlotId == input.Id)
                        connectionsToRemoveWithinSymbol.Add(con);
                }

                foreach (var output in removedOutputDefinitions)
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

            static bool TryCreateInputDefinition(Guid id, string name, bool isMultiInput, Type valueType, Type instanceType, out InputDefinition inputDef)
            {
                // create new input definition
                if (!InputValueCreators.Entries.TryGetValue(valueType, out var creationFunc))
                {
                    BlockingWindow.Instance.ShowMessageBox($"[{instanceType}] can't create Input Definition for "
                                                           + valueType
                                                           + ". You may want to ensure you are using the correct T3 types in your script.", "Slot type error");
                    inputDef = null;
                    return false;
                }

                try
                {
                    inputDef = new InputDefinition { Id = id, Name = name, DefaultValue = creationFunc(), IsMultiInput = isMultiInput };
                    return true;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create default value for {valueType}: {e}");
                    inputDef = null;
                    return false;
                }
            }
        }

    }
    
    private static void UpdateSymbolChildIO(Child symbolChild, SlotChangeInfo slotChangeInfo)
    {
        // update inputs
        var childInputDict = symbolChild.Inputs;
        foreach (var inputDefinition in slotChangeInfo.RemovedInputDefinitions)
        {
            if (!childInputDict.Remove(inputDefinition.Id))
            {
                Log.Error($"Could not remove input {inputDefinition.Name} from {symbolChild.Symbol.Name}");
            }
        }
        
        foreach (var inputDefinition in slotChangeInfo.LatestInputDefinitions)
        {
            var inputId = inputDefinition.Id;
            // create new input if needed
            if (!childInputDict.TryGetValue(inputId, out var input) || input.InputDefinition.DefaultValue.ValueType != inputDefinition.DefaultValue.ValueType)
            {
                input = new Child.Input(inputDefinition);
            }

            childInputDict[inputId] = input;
        }

        // update output of symbol child
        var childOutputDict = symbolChild.Outputs;
        
        foreach(var outputDefinition in slotChangeInfo.RemovedOutputDefinitions)
        {
            if (!childOutputDict.Remove(outputDefinition.Id))
            {
                Log.Error($"Could not remove output {outputDefinition.Name} from {symbolChild.Symbol.Name}");
            }
        }
        
        foreach (var outputDefinition in slotChangeInfo.LatestOutputDefinitions)
        {
            var id = outputDefinition.Id;

            // create new output if needed
            if (!childOutputDict.TryGetValue(id, out var output) || output.OutputDefinition.ValueType != outputDefinition.ValueType)
            {
                OutputDefinition.TryGetNewValueType(outputDefinition, out var outputData);
                output = new Child.Output(outputDefinition, outputData);
            }

            childOutputDict[id] = output;
        }
    }

    private class ConnectionEntry
    {
        public Connection Connection { get; set; }
        public int MultiInputIndex { get; set; }
    }


    internal readonly record struct SlotChangeInfo(
        IReadOnlyList<InputDefinition> RemovedInputDefinitions,
        IReadOnlyList<OutputDefinition> RemovedOutputDefinitions,
        IReadOnlyList<InputDefinition> LatestInputDefinitions,
        IReadOnlyList<OutputDefinition> LatestOutputDefinitions);

    public void ReplaceWithContentsOf(Symbol newSymbol)
    {
        if (newSymbol != this)
        {
            Connections.Clear();
            Connections.AddRange(newSymbol.Connections);
            _children = newSymbol._children;
        }

        SymbolRegistry.SymbolsByType[InstanceType] = this; // todo: ugly - the other one replaced this value with itself when it was created
    }
}