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
                Im.EmptyWindowMessage("Nothing selected");
                return;
            }

            var opNamespace = op.Symbol.Namespace != null
                ? op.Symbol.Namespace
                : "undefined";


            ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
            ImGui.SetNextItemWidth(150);
            var namespaceForEdit = op.Symbol.Namespace;
            if (namespaceForEdit == null)
                namespaceForEdit = "";

            if (ImGui.InputText("##namespace", ref namespaceForEdit, 128))
            {
                _symbolNamespaceCommandInFlight.NewNamespace = namespaceForEdit;
                _symbolNamespaceCommandInFlight.Do();
            }
            if (ImGui.IsItemActivated())
            {
                _symbolNamespaceCommandInFlight = new ChangeSymbolNamespaceCommand(op.Symbol);
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                UndoRedoStack.Add(_symbolNamespaceCommandInFlight);
                _symbolNamespaceCommandInFlight = null;
            }
            ImGui.PopStyleColor();



            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            var nameForEdit = op.Symbol.Name;
            if (ImGui.InputText("##symbolname", ref nameForEdit, 128))
            {
                _symbolNameCommandInFlight.NewName = nameForEdit;
                _symbolNameCommandInFlight.Do();
            }
            if (ImGui.IsItemActivated())
            {
                _symbolNameCommandInFlight = new ChangeSymbolNameCommand(op.Symbol);
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                UndoRedoStack.Add(_symbolNameCommandInFlight);
                _symbolNameCommandInFlight = null;
            }




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
                        _inputValueCommandInFlight = new ChangeInputValueCommand(op.Parent.Symbol, op.Id, input.Input);
                        break;

                    // update command in flight
                    case InputEditState.Modified:
                        Log.Debug("updated 'ChangeInputValue' command");
                        _inputValueCommandInFlight.Value.Assign(input.Input.Value);
                        break;

                    // add command to undo stack
                    case InputEditState.Finished:
                        Log.Debug("Finalized 'ChangeInputValue' command");
                        UndoRedoStack.Add(_inputValueCommandInFlight);
                        break;

                    // update and add command to undo queue
                    case InputEditState.ModifiedAndFinished:
                        Log.Debug("Updated and finalized 'ChangeInputValue' command");
                        _inputValueCommandInFlight.Value.Assign(input.Input.Value);
                        UndoRedoStack.Add(_inputValueCommandInFlight);
                        break;
                }

                ImGui.PopID();
            }
        }

        private ChangeSymbolNameCommand _symbolNameCommandInFlight = null;
        private ChangeSymbolNamespaceCommand _symbolNamespaceCommandInFlight = null;
        private ChangeInputValueCommand _inputValueCommandInFlight = null;
        private readonly string _name;
    }
}
