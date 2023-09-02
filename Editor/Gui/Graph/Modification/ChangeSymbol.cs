
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.CodeAnalysis;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Selection;
using T3.Editor.Gui.Commands.Graph;


namespace T3.Editor.Gui.Graph.Modification;

internal static class ChangeSymbol
{
    public static SymbolChild ChangeOperatorSymbol(SymbolChildUi symbolChildUi, Symbol newSymbol)
    {
        var symbolChild = symbolChildUi.SymbolChild;
        if(symbolChild.Symbol == newSymbol)
            return null;

        var orgPos = symbolChildUi.PosOnCanvas;
        var parentSymbolUi = SymbolUiRegistry.Entries[symbolChild.Parent.Id];

        var conversionWasLossy = false;

        // move old SymbolChild to new position
        var moveCmd = new ModifyCanvasElementsCommand(parentSymbolUi, new List<ISelectableCanvasObject>() { symbolChildUi });
        symbolChildUi.PosOnCanvas = orgPos + new Vector2(0, 100);
        moveCmd.StoreCurrentValues();
        UndoRedoStack.AddAndExecute(moveCmd);

        // create new SymbolChild at original position
        var addSymbolChildCommand = new AddSymbolChildCommand(symbolChild.Parent, newSymbol.Id) { PosOnCanvas = orgPos, ChildName = symbolChild.Name };
        UndoRedoStack.AddAndExecute(addSymbolChildCommand);
        var newSymbolChild = symbolChild.Parent.Children.Single(entry => entry.Id == addSymbolChildCommand.AddedChildId);

        // loop though inputs
        foreach (var input in symbolChild.Inputs)
        {
            var connections = symbolChild.Parent.Connections.FindAll(c => c.TargetSlotId == input.Key
                                                                    && c.TargetParentOrChildId == symbolChild.Id);

            var inputName = input.Value.Name;
            var destInput = newSymbolChild.Inputs.FirstOrDefault(x => x.Value.Name == inputName);

            if (connections.Count > 1) // can this happen?
            {
                Log.Warning("\"Change Symbol...\" : connections.Count > 1 : not yet implemented, skipping!");
                System.Diagnostics.Debug.Assert(false);
            }
            else 
            {
                var inputHasData = connections.Count > 0 || !input.Value.IsDefault;

                if (destInput.Value != null)
                {
                    if(input.Value.Value.ValueType == destInput.Value.Value.ValueType)
                    {
                        // treating default value as a special assignment,
                        // this is also how it is indicated in the UI.
                        // Just leave the initial value of newSymbol input
                        // (assuming this is same as default)

                        if (!input.Value.IsDefault) 
                        {
                            var changeValCommand = new ChangeInputValueCommand(symbolChild.Parent, newSymbolChild.Id, destInput.Value, input.Value.Value);
                            UndoRedoStack.AddAndExecute(changeValCommand);
                        }

                        if (connections.Count == 1)
                        {
                            // remove old connection to symbolChild

                            var delCommand = new DeleteConnectionCommand(symbolChild.Parent, connections[0], 0);
                            UndoRedoStack.AddAndExecute(delCommand);


                            // add new connection to newSymbolChild

                            var newConnectionToInput = new Symbol.Connection(
                                                    sourceParentOrChildId: connections[0].SourceParentOrChildId,
                                                    sourceSlotId: connections[0].SourceSlotId,
                                                    targetParentOrChildId: newSymbolChild.Id,
                                                    targetSlotId: destInput.Key);
                            var addCommand = new AddConnectionCommand(symbolChild.Parent, newConnectionToInput, 0);
                            UndoRedoStack.AddAndExecute(addCommand);
                        }
                    }
                    else if(inputHasData)
                    {
                        conversionWasLossy = true;
                        Log.Info("\"Change Symbol...\" : type mismatching, input:" + inputName + ", " + input.Value.Value.ValueType + "(old) vs " + destInput.Value.Value.ValueType + "(new)");
                    }

                }
                else if(inputHasData)
                {
                    conversionWasLossy = true;
                    Log.Info("\"Change Symbol...\" : no matching input, name: " + inputName);
                }

            }

        }


        // loop though outputs
        foreach (var output in symbolChild.Outputs)
        {
            var connections = symbolChild.Parent.Connections.FindAll(c => c.SourceSlotId == output.Key
                                                                    && c.SourceParentOrChildId == symbolChild.Id);

            var outputName = output.Value.OutputDefinition.Name;
            var destOutput = newSymbolChild.Outputs.FirstOrDefault(x => x.Value.OutputDefinition.Name == outputName);

            foreach (var connection in connections)
            {
                if (destOutput.Value != null)
                {
                    if (output.Value.OutputDefinition.ValueType == destOutput.Value.OutputDefinition.ValueType)
                    {
                        // remove old connection from symbolChild

                        var multiInputIndex = symbolChild.Parent.GetMultiInputIndexFor(connection);
                        var delCommand = new DeleteConnectionCommand(symbolChild.Parent, connection, multiInputIndex);
                        UndoRedoStack.AddAndExecute(delCommand);


                        // add new connection from newSymbolChild

                        var newConnectionToInput = new Symbol.Connection(
                                             sourceParentOrChildId: newSymbolChild.Id,
                                             sourceSlotId: destOutput.Key,
                                             targetParentOrChildId: connection.TargetParentOrChildId,
                                             targetSlotId: connection.TargetSlotId);
                        var addCommand = new AddConnectionCommand(symbolChild.Parent, newConnectionToInput, multiInputIndex);
                        UndoRedoStack.AddAndExecute(addCommand);
                    }
                    else
                    {
                        conversionWasLossy = true;
                        Log.Info("\"Change Symbol...\" : type mismatching, input:" + outputName + ", " + output.Value.OutputDefinition.ValueType + "(old) vs " + destOutput.Value.OutputDefinition.ValueType + "(new)");
                    }
                }
                else
                {
                    conversionWasLossy = true;
                    Log.Info("\"Change Symbol...\" : no matching output, name: " + outputName + " (connection left as is)");
                }
            }
        }

        if(!conversionWasLossy)
        {
            var delCommand = new DeleteSymbolChildrenCommand(parentSymbolUi, new List<SymbolChildUi>() { symbolChildUi });
            UndoRedoStack.AddAndExecute(delCommand);
        }
        else
        {
            Log.Warning("\"Change Symbol...\" : old operator not deleted due to lossy conversion");
        }

        return newSymbolChild;
    }
}