using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph.Interaction;

internal static class ParameterExtraction
{
    public static bool IsInputSlotExtractable(IInputSlot inputSlot)
    {
        return SymbolsExtractableFromInputs.ContainsKey(inputSlot.ValueType);
    }
    
    public static void ExtractAsConnectedOperator<T>(NodeSelection nodeSelection, InputSlot<T> inputSlot, SymbolUi.Child symbolChildUi, Symbol.Child.Input input)
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

        // cast input slot to constructedInputSlotType
        // Find matching symbol
        if (!SymbolsExtractableFromInputs.TryGetValue(input.DefaultValue.ValueType, out var symbolId))
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
            newChildUi.Style = SymbolUi.Child.Styles.Resizable;
        }

        // Set type
        // Todo - make this undoable - currently not implemented with the new extraction system
        var newInstance = composition.Children[newChildUi.Id];
        ExtractInputValues(inputSlot, newInstance, out var outputSlot, out var previousValue);

        // Create connection
        var newConnection = new Symbol.Connection(sourceParentOrChildId: newSymbolChild.Id,
                                                  sourceSlotId: outputSlot.Id,
                                                  targetParentOrChildId: symbolChildUi.SymbolChild.Id,
                                                  targetSlotId: input.Id);
        var addConnectionCommand = new AddConnectionCommand(compositionUi.Symbol, newConnection, 0);
        addConnectionCommand.Do();
        commands.Add(addConnectionCommand);
        UndoRedoStack.Add(new MacroCommand("Extract as operator", commands));
        return;

        static void ExtractInputValues(InputSlot<T> slot, Instance newInstance, out Slot<T> outputSlot, out T previousValue)
        {
            var extractableInput = (IExtractedInput<T>)newInstance;
            outputSlot = extractableInput.OutputSlot;
            previousValue = outputSlot.Value;
            extractableInput.SetInputValues(slot.TypedInputValue.Value);
        }
    }

    private static readonly Dictionary<Type, System.Numerics.Vector2> _sizesForTypes = new()
                                                                                           {
                                                                                               { typeof(string), new System.Numerics.Vector2(120, 80) },
                                                                                           };
    
    // todo: define this elsewhere so they can be properly hot reloaded
    private static Dictionary<Type, Guid>? _symbolsExtractableFromInputs;
    private static Dictionary<Type, Guid> SymbolsExtractableFromInputs => _symbolsExtractableFromInputs ??=
                                                                              SymbolPackage.AllPackages
                                                                                           .SelectMany(package =>
                                                                                                       {
                                                                                                           return package.Symbols.Values
                                                                                                              .Select(x =>
                                                                                                               {
                                                                                                                   var typeInfo = package
                                                                                                                      .AssemblyInformation
                                                                                                                      .OperatorTypeInfo[x.Id];
                                                                                                                   return (x, typeInfo);
                                                                                                               });
                                                                                                       })
                                                                                           .Where(x => x.typeInfo.ExtractableTypeInfo.IsExtractable)
                                                                                           .ToDictionary(x => x.typeInfo.ExtractableTypeInfo.ExtractableType,
                                                                                                         x => x.x.Id);

}