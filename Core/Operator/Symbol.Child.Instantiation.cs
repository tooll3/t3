using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using T3.Core.Compilation;
using T3.Core.Logging;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator;

public static class SymbolInstantiation
{
    internal static bool TryCreateNewInstance(this Symbol.Child symbolChild, Instance parentInstance, [NotNullWhen(true)] out Instance newInstance)
    {
        if (!TryCreateInstance(parentInstance, symbolChild, out newInstance, out var reason))
        {
            Log.Error(reason);
            return false;
        }

        var newInstanceSymbol = symbolChild.Symbol;
        newInstanceSymbol.AddInstanceOfSelf(newInstance);

        if (parentInstance != null)
        {
            Instance.AddChildTo(parentInstance, newInstance);
        }

        // cache property accesses for performance
        var newInstanceInputDefinitions = newInstanceSymbol.InputDefinitions;
        var newInstanceInputDefinitionCount = newInstanceInputDefinitions.Count;

        var newInstanceInputs = newInstance.Inputs;
        var newInstanceInputCount = newInstanceInputs.Count;

        var symbolChildInputs = symbolChild.Inputs;

        // set up the inputs for the child instance
        for (int i = 0; i < newInstanceInputDefinitionCount; i++)
        {
            if (i >= newInstanceInputCount)
            {
                Log.Warning($"Skipping undefined input index");
                continue;
            }

            var inputDefinitionId = newInstanceInputDefinitions[i].Id;
            var inputSlot = newInstanceInputs[i];
            if (!symbolChildInputs.TryGetValue(inputDefinitionId, out var input))
            {
                Log.Warning($"Skipping undefined input: {inputDefinitionId}");
                continue;
            }

            inputSlot.Input = input;
            inputSlot.Id = inputDefinitionId;
        }

        // cache property accesses for performance
        var childOutputDefinitions = newInstanceSymbol.OutputDefinitions;
        var childOutputDefinitionCount = childOutputDefinitions.Count;

        var childOutputs = newInstance.Outputs;

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

        if (symbolChild.IsBypassed)
        {
            symbolChild.IsBypassed = true;
        }

        return true;

        static bool TryCreateInstance(Instance parent, Symbol.Child symbolChild, [NotNullWhen(true)] out Instance newInstance, out string reason)
        {
            var symbolChildId = symbolChild.Id;

            if (parent != null && parent.Children.ContainsKey(symbolChildId))
            {
                reason = $"Instance {symbolChild} with id ({symbolChild.Id}) already exists in {parent.Symbol}";
                newInstance = null;
                return false;
            }

            var symbolOfNewInstance = symbolChild.Symbol;
            if (!TryInstantiate(out newInstance, symbolOfNewInstance, symbolChildId, out reason))
            {
                Log.Error(reason);
                return false;
            }

            Debug.Assert(newInstance != null);
            newInstance!.SetChildId(symbolChild.Id);

            newInstance.Parent = parent;

            Instance.SortInputSlotsByDefinitionOrder(newInstance);

            // populates child instances of the new instance
            foreach (var child in symbolOfNewInstance.Children.Values)
            {
                var success = TryCreateNewInstance(child, newInstance, out _);
            }

            // create connections between child instances populated with CreateAndAddNewChildInstance
            var connections = symbolOfNewInstance.Connections;

            // if connections already exist for the symbol, remove any that shouldn't exist anymore
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

                    if (!newInstance.TryAddConnection(connection, count))
                    {
                        Log.Warning($"Removing obsolete connecting in {symbolOfNewInstance}...");
                        connections.RemoveAt(index);
                        index--;
                        continue;
                    }

                    conHashToCount[hash] = count + 1;
                }
            }

            // connect animations if available
            symbolOfNewInstance.Animator.CreateUpdateActionsForExistingCurves(newInstance.Children.Values);

            return true;

            static bool TryInstantiate([NotNullWhen(true)] out Instance instance, Symbol symbol, in Guid id, out string reason)
            {
                var symbolPackage = symbol.SymbolPackage;
                if (symbolPackage.AssemblyInformation.OperatorTypeInfo.TryGetValue(symbol.Id, out var typeInfo))
                {
                    var constructor = typeInfo.GetConstructor();
                    try
                    {
                        instance = (Instance)constructor.Invoke();
                        reason = string.Empty;
                        return true;
                    }
                    catch (Exception e)
                    {
                        reason = $"Failed to create instance of type {symbol.InstanceType} with id {id}: {e}";
                        instance = null;
                        return false;
                    }
                }

                Log.Error($"No constructor found for {symbol.InstanceType}. This should never happen!! Please report this");

                try
                {
                    // create instance through reflection
                    instance = (Instance)Activator.CreateInstance(symbol.InstanceType, AssemblyInformation.ConstructorBindingFlags,
                                                                  binder: null, args: Array.Empty<object>(), culture: null);

                    if (instance is null)
                    {
                        reason = $"(Instance creation fallback failure) Failed to create instance of type " +
                                 $"{symbol.InstanceType} with id {id} - result was null";
                        return false;
                    }

                    Log.Warning($"(Instance creation fallback) Created instance of type {symbol.InstanceType} with id {id} through reflection");

                    reason = string.Empty;
                    return true;
                }
                catch (Exception e)
                {
                    reason = $"(Instance creation fallback failure) Failed to create instance of type {symbol.InstanceType} with id {id}: {e}";
                    instance = null;
                    return false;
                }
            }
        }
    }
}