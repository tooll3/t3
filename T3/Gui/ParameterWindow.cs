using ImGuiNET;
using System.Linq;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;

namespace T3.Gui
{
    /// <summary>
    /// 
    /// </summary>
    class ParameterWindow
    {
        public static void Draw(Instance compositionOp, SymbolChildUi childUi)
        {
            ImGui.Begin("ParameterView");
            {
                if (childUi != null)
                    DrawParameters(compositionOp, childUi);
            }
            ImGui.End();
        }

        public static void DrawParameters(Instance compositionOp, SymbolChildUi selectedChildUi)
        {
            if (selectedChildUi == null || compositionOp == null)
                return;

            var symbolChild = selectedChildUi.SymbolChild;
            var selectedInstance = compositionOp.Children.SingleOrDefault(child => child.Id == symbolChild.Id);
            if (selectedInstance == null)
                return;
            var selectedSymbolUi = SymbolUiRegistry.Entries[selectedInstance.Symbol.Id]; 

            foreach (var input in selectedInstance.Inputs)
            {
                ImGui.PushID(input.Id.GetHashCode());
                IInputUi inputUi = selectedSymbolUi.InputUis[input.Id];

                var editState = inputUi.DrawInputEdit(input.Input.InputDefinition.Name, input);

                switch (editState)
                {
                    case InputEditState.Focused:
                        // create command for possible editing
                        Log.Debug("setup 'ChangeInputValue' command");
                        UndoRedoStack.CommandInFlight = new ChangeInputValueCommand(compositionOp.Symbol, symbolChild.Id, input.Input);
                        break;
                    case InputEditState.Modified:
                        // update command in flight
                        Log.Debug("updated 'ChangeInputValue' command");
                        ((ChangeInputValueCommand)UndoRedoStack.CommandInFlight).Value.Assign(input.Input.Value); // todo: ugly!
                        break;
                    case InputEditState.Finished:
                        // add command to undo stack
                        Log.Debug("Finalized 'ChangeInputValue' command");
                        UndoRedoStack.AddCommandInFlightToStack();
                        break;
                    case InputEditState.ModifiedAndFinished:
                        // update and add command to undo queue
                        Log.Debug("Updated and finalized 'ChangeInputValue' command");
                        ((ChangeInputValueCommand)UndoRedoStack.CommandInFlight).Value.Assign(input.Input.Value); // todo: ugly!
                        UndoRedoStack.AddCommandInFlightToStack();
                        break;
                }

                ImGui.PopID();
            }
        }
    }
}
