using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using UiHelpers;

namespace T3.Gui.Windows
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


        public bool Draw(Instance op, SymbolChildUi symbolChildUi)
        {
            var isOpen = true;
            ImGui.Begin(_name, ref isOpen);
            {
                DrawContent(op, symbolChildUi);
            }
            ImGui.End();

            return isOpen;
        }

        //public bool Draw()



        private void DrawContent(Instance op, SymbolChildUi symbolChildUi)
        {
            if (op == null)
            {
                Im.EmptyWindowMessage("Nothing selected");
                return;
            }


            var opNamespace = op.Symbol.Namespace ?? "undefined";

            // Namespace
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
                ImGui.SetNextItemWidth(150);
                var namespaceForEdit = op.Symbol.Namespace ?? "";

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
            }


            // Symbol Name
            {
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
            }

            // SymbolChild Name
            {
                ImGui.SetNextItemWidth(-1);
                var nameForEdit = symbolChildUi.SymbolChild.Name;
                if (ImGui.InputText("##symbolchildname", ref nameForEdit, 128))
                {
                    _symbolChildNameCommand.NewName = nameForEdit;
                    symbolChildUi.SymbolChild.Name = nameForEdit;
                }
                if (ImGui.IsItemActivated())
                {
                    _symbolChildNameCommand = new ChangeSymbolChildNameCommand(symbolChildUi, op.Parent.Symbol);
                }
                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    UndoRedoStack.Add(_symbolChildNameCommand);
                    _symbolChildNameCommand = null;
                }
            }

            //ImGui.Spacing();
            ImGui.Dummy(new Vector2(0.0f, 5.0f));


            var compositionSymbolUi = SymbolUiRegistry.Entries[op.Parent.Symbol.Id];
            var selectedChildSymbolUi = SymbolUiRegistry.Entries[op.Symbol.Id];

            foreach (var input in op.Inputs)
            {
                ImGui.PushID(input.Id.GetHashCode());
                IInputUi inputUi = selectedChildSymbolUi.InputUis[input.Id];

                if (_showInputParameterEdits.SymbolHash == op.Symbol.Id.GetHashCode())
                {
                    if (_showInputParameterEdits.InputHash == input.Id.GetHashCode())
                    {
                        inputUi.DrawParameterEdits();
                        if (ImGui.Button("Back"))
                        {
                            _showInputParameterEdits = ShownInputParameterEdit.None;
                        }
                    }
                }
                else
                {
                    var editState = inputUi.DrawInputEdit(input, compositionSymbolUi, symbolChildUi);

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

                        case InputEditState.ShowOptions:
                            _showInputParameterEdits = new ShownInputParameterEdit(op.Symbol.Id.GetHashCode(), input.Id.GetHashCode());
                            break;
                    }
                }
                ImGui.PopID();
            }
        }

        struct ShownInputParameterEdit
        {
            public static readonly ShownInputParameterEdit None = new ShownInputParameterEdit(0, 0);
            public ShownInputParameterEdit(int symbolHash, int inputHash)
            {
                SymbolHash = symbolHash;
                InputHash = inputHash;
            }

            public int SymbolHash;
            public int InputHash;
        }

        private ShownInputParameterEdit _showInputParameterEdits = ShownInputParameterEdit.None;
        private ChangeSymbolNameCommand _symbolNameCommandInFlight = null;
        private ChangeSymbolNamespaceCommand _symbolNamespaceCommandInFlight = null;
        private ChangeSymbolChildNameCommand _symbolChildNameCommand = null;
        private ChangeInputValueCommand _inputValueCommandInFlight = null;
        private readonly string _name;
        private Instance _pinnedOp = null;

    }
}
