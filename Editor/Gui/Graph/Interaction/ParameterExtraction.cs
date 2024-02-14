using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_5880cbc3_a541_4484_a06a_0e6f77cdbe8e;
using T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe;
using T3.Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536;
using T3.Operators.Types.Id_cc07b314_4582_4c2c_84b8_bb32f59fc09b;
using Vector2 = T3.Operators.Types.Id_926ab3fd_fbaf_4c4b_91bc_af277000dcb8.Vector2;
using Vector3 = T3.Operators.Types.Id_94a5de3b_ee6a_43d3_8d21_7b8fe94b042b.Vector3;

namespace T3.Editor.Gui.Graph.Interaction;

internal static class ParameterExtraction
{
    public static bool IsInputSlotExtractable(IInputSlot inputSlot)
    {
        return _symbolIdsForTypes.ContainsKey(inputSlot.ValueType);
    }
    
    public static void ExtractAsConnectedOperator(IInputSlot inputSlot, SymbolChildUi symbolChildUi, SymbolChild.Input input)
    {
        var composition = NodeSelection.GetSelectedComposition() ?? inputSlot.Parent.Parent;
        if (composition == null)
        {
            Log.Warning("Can't publish input to undefined composition");
            return;
        }

        var compositionSymbol = composition.Symbol;
        var commands = new List<ICommand>();

        // Find matching symbol
        if (!_symbolIdsForTypes.TryGetValue(input.DefaultValue.ValueType, out var symbolId))
        {
            Log.Warning("Can't extract this parameter type");
            return;
        }

        var symbol = SymbolRegistry.Entries[symbolId];

        // Add Child
        var freePosition = NodeGraphLayouting.FindPositionForNodeConnectedToInput(compositionSymbol, symbolChildUi, input.InputDefinition);
        var addSymbolChildCommand = new AddSymbolChildCommand(compositionSymbol, symbol.Id)
                                        {
                                            PosOnCanvas = freePosition,
                                            ChildName = input.Name
                                        };
        if (_sizesForTypes.TryGetValue(input.DefaultValue.ValueType, out var sizeOverride))
        {
            addSymbolChildCommand.Size = sizeOverride;  // FIXME: doesn't seem to have an effect
        }

        commands.Add(addSymbolChildCommand);
        addSymbolChildCommand.Do();

        var newSymbolChild = compositionSymbol.Children.Single(entry => entry.Id == addSymbolChildCommand.AddedChildId);

        var symbolUi = SymbolUiRegistry.Entries[compositionSymbol.Id];
        var newChildUi = symbolUi.ChildUis.Find(s => s.Id == newSymbolChild.Id);

        // Sadly, we have have apply size manually.
        if (_sizesForTypes.TryGetValue(input.DefaultValue.ValueType, out _))
        {
            newChildUi.Style = SymbolChildUi.Styles.Resizable;
        }

        if (newChildUi == null)
        {
            Log.Warning("Unable to create new operator");
            return;
        }

        // Set type
        var newInstance = composition.Children.Single(child => child.SymbolChildId == newChildUi.Id);
        var inputsAndValues = new Dictionary<SymbolChild.Input, InputValue>();

        switch (newInstance)
        {
            case Value valueInstance when inputSlot is InputSlot<float> floatInput:
            {
                inputsAndValues[valueInstance.Float.Input] = floatInput.TypedInputValue;
                break;
            }
            case IntValue intValueInstance when inputSlot is InputSlot<int> intInput:
            {
                inputsAndValues[intValueInstance.Int.Input] = intInput.TypedInputValue;
                break;
            }

            case AString stringInstance when inputSlot is InputSlot<string> stringInput:
            {
                inputsAndValues[stringInstance.InputString.Input] = stringInput.TypedInputValue;
                break;
            }

            case SampleGradient gradientInstance when inputSlot is InputSlot<Gradient> gradientInput:
            {
                inputsAndValues[gradientInstance.Gradient.Input] = gradientInput.TypedInputValue;
                break;
            }
                
            case Vector2 float2ToVector2 when inputSlot is InputSlot<System.Numerics.Vector2> vec2:
            {
                inputsAndValues[float2ToVector2.X.Input] = new InputValue<float>(vec2.TypedInputValue.Value.X);
                inputsAndValues[float2ToVector2.Y.Input] = new InputValue<float>(vec2.TypedInputValue.Value.Y);
                break;
            }
                
            case Vector3 float3ToVector3 when inputSlot is InputSlot<System.Numerics.Vector3> vec3:
            {
                inputsAndValues[float3ToVector3.X.Input] = new InputValue<float>(vec3.TypedInputValue.Value.X);
                inputsAndValues[float3ToVector3.Y.Input] = new InputValue<float>(vec3.TypedInputValue.Value.Y);
                inputsAndValues[float3ToVector3.Z.Input] = new InputValue<float>(vec3.TypedInputValue.Value.Z);
                break;
            }
        }

        if (inputsAndValues.Count == 0)
        {
            Log.Warning("Failed to find matching types");
            return;
        }

        foreach (var (typedInput, inputValue) in inputsAndValues)
        {
            var setValueCommand = new ChangeInputValueCommand(compositionSymbol, newSymbolChild.Id, typedInput, inputValue);
            setValueCommand.Do();
            commands.Add(setValueCommand);
        }

        // Create connection
        var firstMatchingOutput = newSymbolChild.Symbol.OutputDefinitions.First(o => o.ValueType == input.DefaultValue.ValueType);

        var newConnection = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                  sourceSlotId: firstMatchingOutput.Id,
                                                  targetParentOrChildId: symbolChildUi.SymbolChild.Id,
                                                  targetSlotId: input.InputDefinition.Id);
        var addConnectionCommand = new AddConnectionCommand(compositionSymbol, newConnection, 0);
        addConnectionCommand.Do();
        commands.Add(addConnectionCommand);
        UndoRedoStack.Add(new MacroCommand("Extract as operator", commands));
    }

    private static readonly Dictionary<Type, Guid> _symbolIdsForTypes = new()
                                                                            {
                                                                                { typeof(float), Guid.Parse("5d7d61ae-0a41-4ffa-a51d-93bab665e7fe") },
                                                                                { typeof(System.Numerics.Vector2), Guid.Parse("926ab3fd-fbaf-4c4b-91bc-af277000dcb8") },
                                                                                { typeof(System.Numerics.Vector3), Guid.Parse("94a5de3b-ee6a-43d3-8d21-7b8fe94b042b") },
                                                                                { typeof(string), Guid.Parse("5880cbc3-a541-4484-a06a-0e6f77cdbe8e") },
                                                                                { typeof(int), Guid.Parse("cc07b314-4582-4c2c-84b8-bb32f59fc09b") },
                                                                                { typeof(Gradient), Guid.Parse("8211249d-7a26-4ad0-8d84-56da72a5c536") },
                                                                            };

    private static readonly Dictionary<Type, System.Numerics.Vector2> _sizesForTypes = new()
                                                                                           {
                                                                                               { typeof(string), new System.Numerics.Vector2(120, 80) },
                                                                                           };
}