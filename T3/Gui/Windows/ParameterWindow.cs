using ImGuiNET;
using System;
using System.Collections.Generic;
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
    class ParameterWindow : Window
    {
        public ParameterWindow() : base()
        {
            _instanceCounter++;
            Config.Title = "Parameters##" + _instanceCounter;
            AllowMultipleInstances = true;
            Config.Visible = true;

            WindowInstances.Add(this);
        }


        protected override void UpdateBeforeDraw()
        {
            _pinning.UpdateSelection();
        }

        protected override void DrawAllInstances()
        {
            foreach (var w in WindowInstances)
            {
                w.DrawOneInstance();
            }
        }

        protected override void Close()
        {
            WindowInstances.Remove(this);
        }


        protected override void AddAnotherInstance()
        {
            new ParameterWindow();
        }


        protected override void DrawContent()
        {
            var op = _pinning.SelectedInstance;

            if (op == null || _pinning.SelectedChildUi == null)
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
                if (ImGui.InputText("##symbolname", ref nameForEdit, 128, ImGuiInputTextFlags.ReadOnly))
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
                var nameForEdit = _pinning.SelectedChildUi.SymbolChild.Name;
                if (ImGui.InputText("##symbolchildname", ref nameForEdit, 128))
                {
                    _symbolChildNameCommand.NewName = nameForEdit;
                    _pinning.SelectedChildUi.SymbolChild.Name = nameForEdit;
                }
                if (ImGui.IsItemActivated())
                {
                    _symbolChildNameCommand = new ChangeSymbolChildNameCommand(_pinning.SelectedChildUi, op.Parent.Symbol);
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
                    var editState = inputUi.DrawInputEdit(input, compositionSymbolUi, _pinning.SelectedChildUi);

                    switch (editState)
                    {
                        // create command for possible editing
                        case InputEditState.Focused:
                            //Log.Debug("setup 'ChangeInputValue' command");
                            _inputValueCommandInFlight = new ChangeInputValueCommand(op.Parent.Symbol, op.Id, input.Input);
                            break;

                        // update command in flight
                        case InputEditState.Modified:
                            //Log.Debug("updated 'ChangeInputValue' command");
                            _inputValueCommandInFlight.Value.Assign(input.Input.Value);
                            break;

                        // add command to undo stack
                        case InputEditState.Finished:
                            //Log.Debug("Finalized 'ChangeInputValue' command");
                            UndoRedoStack.Add(_inputValueCommandInFlight);
                            break;

                        // update and add command to undo queue
                        case InputEditState.ModifiedAndFinished:
                            //Log.Debug("Updated and finalized 'ChangeInputValue' command");
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
        private readonly SelectionPinning _pinning = new SelectionPinning();
        //private static List<ParameterWindow> WindowInstances = new List<ParameterWindow>();
        static int _instanceCounter = 0;

    }
}
