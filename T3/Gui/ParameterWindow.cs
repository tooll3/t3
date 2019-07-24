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
        public ParameterWindow(string name)
        {
            _name = name;
        }


        public bool Draw(Instance compositionOp, SymbolChildUi childUi)
        {
            var isOpen = true;
            ImGui.Begin(_name, ref isOpen);
            {
                if (childUi != null)
                    DrawParameters(compositionOp, childUi);
            }
            ImGui.End();
            return isOpen;
        }

        private void DrawParameters(Instance compositionOp, SymbolChildUi selectedChildUi)
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

                var editState = inputUi.DrawInputEdit(input);

                switch (editState)
                {
                    // create command for possible editing
                    case InputEditState.Focused:
                        Log.Debug("setup 'ChangeInputValue' command");
                        _commandInFlight = new ChangeInputValueCommand(compositionOp.Symbol, symbolChild.Id, input.Input);
                        break;

                    // update command in flight
                    case InputEditState.Modified:
                        Log.Debug("updated 'ChangeInputValue' command");
                        _commandInFlight.Value.Assign(input.Input.Value);
                        break;

                    // add command to undo stack
                    case InputEditState.Finished:
                        Log.Debug("Finalized 'ChangeInputValue' command");
                        //UndoRedoStack.AddCommandInFlightToStack();
                        UndoRedoStack.Add(_commandInFlight);
                        break;

                    // update and add command to undo queue
                    case InputEditState.ModifiedAndFinished:
                        Log.Debug("Updated and finalized 'ChangeInputValue' command");
                        _commandInFlight.Value.Assign(input.Input.Value);
                        //UndoRedoStack.AddCommandInFlightToStack();
                        UndoRedoStack.Add(_commandInFlight);
                        break;
                }
                ImGui.PopID();
            }
        }
        private ChangeInputValueCommand _commandInFlight = null;
        private string _name;

    }
}
