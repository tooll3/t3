using ImGuiNET;
using imHelpers;
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


        public bool Draw(Instance op)
        {
            var isOpen = true;
            ImGui.Begin(_name, ref isOpen);
            {
                DrawContent(op);
            }
            ImGui.End();

            return isOpen;
        }


        private Instance _pinnedOp = null;

        private string _testString = "hallo";

        private void DrawContent(Instance op)
        {
            if (_pinnedOp != null && _pinnedOp.Parent.Children.Contains(_pinnedOp))
            {
                _pinnedOp = null;
            }

            if (_pinnedOp != null)
                op = _pinnedOp;

            if (op == null)
            {
                //ImGui.Text("Nothing selected")
                Im.EmptyWindowMessage("Nothing selected");
                return;
            }

            var opNamespace = op.Symbol.Namespace != null
                ? op.Symbol.Namespace
                : "undefined";

            if (ImGui.InputText("##234", ref _testString, 128))
            {
                //_testString = new string()
            }

            ImGui.InputTextWithHint("", "asdf", op.Symbol.Name, 128);

            var selectedSymbolUi = SymbolUiRegistry.Entries[op.Symbol.Id];

            foreach (var input in op.Inputs)
            {
                ImGui.PushID(input.Id.GetHashCode());
                IInputUi inputUi = selectedSymbolUi.InputUis[input.Id];

                var editState = inputUi.DrawInputEdit(input);

                switch (editState)
                {
                    // create command for possible editing
                    case InputEditState.Focused:
                        Log.Debug("setup 'ChangeInputValue' command");
                        _commandInFlight = new ChangeInputValueCommand(op.Parent.Symbol, op.Id, input.Input);
                        break;

                    // update command in flight
                    case InputEditState.Modified:
                        Log.Debug("updated 'ChangeInputValue' command");
                        _commandInFlight.Value.Assign(input.Input.Value);
                        break;

                    // add command to undo stack
                    case InputEditState.Finished:
                        Log.Debug("Finalized 'ChangeInputValue' command");
                        UndoRedoStack.Add(_commandInFlight);
                        break;

                    // update and add command to undo queue
                    case InputEditState.ModifiedAndFinished:
                        Log.Debug("Updated and finalized 'ChangeInputValue' command");
                        _commandInFlight.Value.Assign(input.Input.Value);
                        UndoRedoStack.Add(_commandInFlight);
                        break;
                }

                ImGui.PopID();
            }
        }

        private ChangeInputValueCommand _commandInFlight = null;
        private readonly string _name;
    }
}
