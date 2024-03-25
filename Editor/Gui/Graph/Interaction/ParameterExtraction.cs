using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Windows;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction;

internal static class ParameterExtraction
{
    public static bool IsInputSlotExtractable(IInputSlot inputSlot)
    {
        return _symbolIdsForTypes.ContainsKey(inputSlot.ValueType);
    }
    
    public static void ExtractAsConnectedOperator(NodeSelection nodeSelection, IInputSlot inputSlot, SymbolChildUi symbolChildUi, SymbolChild.Input input)
    {
        SymbolUi? compositionUi = null;
        Instance composition;
        var potentialComposition = nodeSelection.GetSelectedComposition();
        if (potentialComposition != null)
        {
            compositionUi = potentialComposition.GetSymbolUi();
            composition = potentialComposition;
        }
        else
        {
            composition = inputSlot.Parent.Parent;
        }
        
        if (composition == null)
        {
            Log.Warning("Can't publish input to undefined composition");
            return;
        }

        if (compositionUi == null)
        {
            compositionUi = composition.GetSymbolUi();
        }
            

        var compositionSymbol = composition.Symbol;
        var commands = new List<ICommand>();

        // Find matching symbol
        if (!_symbolIdsForTypes.TryGetValue(input.DefaultValue.ValueType, out var symbolId))
        {
            Log.Warning("Can't extract this parameter type");
            return;
        }

        // Add Child
        var freePosition = NodeGraphLayouting.FindPositionForNodeConnectedToInput(compositionSymbol, symbolChildUi);
        var addSymbolChildCommand = new AddSymbolChildCommand(composition.Symbol, symbolId)
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


        var newChildUi = compositionUi.ChildUis[addSymbolChildCommand.AddedChildId];
        var newSymbolChild = newChildUi.SymbolChild;

        // Sadly, we have have apply size manually.
        if (_sizesForTypes.TryGetValue(input.DefaultValue.ValueType, out _))
        {
            newChildUi.Style = SymbolChildUi.Styles.Resizable;
        }

        // Set type
        var newInstance = composition.Children[newChildUi.Id];

        if(newInstance is not IExtractable extractable) // FIXME: implement extractable - got lost in source control
        {
            Log.Warning("Can't extract this parameter type");
            return;
        }
        
        var inputsAndValues = new Dictionary<SymbolChild.Input, InputValue>();
        
        var success = extractable.TryExtractInputsFor(inputSlot, out var extractedInputs);
        
        if (!success)
        {
            Log.Warning("Failed to find matching types");
            return;
        }
        
        foreach (var extractedInput in extractedInputs)
        {
            inputsAndValues[extractedInput.InstanceInput] = extractedInput.InputValue;
        }

        if (inputsAndValues.Count == 0)
        {
            return;
        }

        foreach (var (typedInput, inputValue) in inputsAndValues)
        {
            var setValueCommand = new ChangeInputValueCommand(newSymbolChild.Symbol, newSymbolChild.Id, typedInput, inputValue);
            setValueCommand.Do();
            commands.Add(setValueCommand);
        }

        // Create connection
        var firstMatchingOutput = newSymbolChild.Symbol.OutputDefinitions.First(o => o.ValueType == input.DefaultValue.ValueType);

        var newConnection = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                  sourceSlotId: firstMatchingOutput.Id,
                                                  targetParentOrChildId: symbolChildUi.SymbolChild.Id,
                                                  targetSlotId: input.Id);
        var addConnectionCommand = new AddConnectionCommand(compositionUi.Symbol, newConnection, 0);
        addConnectionCommand.Do();
        commands.Add(addConnectionCommand);
        UndoRedoStack.Add(new MacroCommand("Extract as operator", commands));
    }

    // Todo: this should be defined where the types are defined and be direct symbol references
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