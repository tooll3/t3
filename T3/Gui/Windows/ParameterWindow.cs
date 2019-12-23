using ImGuiNET;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Windows
{
    class ParameterWindow : Window
    {
        public ParameterWindow()
        {
            _instanceCounter++;
            Config.Title = "Parameters##" + _instanceCounter;
            AllowMultipleInstances = true;
            Config.Visible = true;

            ParameterWindowInstances.Add(this);
        }

        public override List<Window> GetInstances()
        {
            return ParameterWindowInstances;
        }

        protected override void UpdateBeforeDraw()
        {
            _pinning.UpdateSelection();
        }

        protected override void DrawAllInstances()
        {
            foreach (var w in ParameterWindowInstances)
            {
                w.DrawOneInstance();
            }
        }

        protected override void Close()
        {
            ParameterWindowInstances.Remove(this);
        }

        protected override void AddAnotherInstance()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new ParameterWindow(); // Required to call constructor
        }

        protected override void DrawContent()
        {
            var instance = SelectionManager.GetSelectedInstance();
            if (instance != null)
            { 
                DrawSelectedSymbolHeader(instance);

                var compositionSymbolUi = SymbolUiRegistry.Entries[instance.Parent.Symbol.Id];
                var selectedChildSymbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
                
                // Draw parameters
                foreach (var inputSlot in instance.Inputs)
                {
                    if (!selectedChildSymbolUi.InputUis.TryGetValue(inputSlot.Id, out IInputUi inputUi))
                    {
                        Log.Warning("Trying to access an non existing input, probably the op instance is not the actual one.");
                        continue;
                    }

                    ImGui.PushID(inputSlot.Id.GetHashCode());
                    var editState = inputUi.DrawInputEdit(inputSlot, compositionSymbolUi, _pinning.SelectedChildUi);

                    if ((editState & InputEditState.Started) != InputEditState.Nothing)
                    {
                        _inputValueCommandInFlight = new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input);
                    }

                    if ((editState & InputEditState.Modified) != InputEditState.Nothing)
                    {
                        if (_inputValueCommandInFlight == null || _inputValueCommandInFlight.Value.ValueType != inputSlot.Input.Value.ValueType)
                            _inputValueCommandInFlight = new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input);
                        _inputValueCommandInFlight.Value.Assign(inputSlot.Input.Value);
                    }

                    if ((editState & InputEditState.Finished) != InputEditState.Nothing)
                    {
                        if (_inputValueCommandInFlight != null && _inputValueCommandInFlight.Value.ValueType == inputSlot.Input.Value.ValueType)
                            UndoRedoStack.Add(_inputValueCommandInFlight);
                    }

                    if (editState == InputEditState.ShowOptions)
                    {
                        SelectionManager.SetSelection(inputUi);
                    }

                    ImGui.PopID();
                }
                return;
            }

            if (SelectionManager.Selection.Count == 0)
                return;

            if (SelectionManager.Selection[0] is IInputUi inputUiForSettings)
            {
                inputUiForSettings.DrawSettings();
            }
        }

        private void DrawSelectedSymbolHeader(Instance op)
        {
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
                if (ImGui.InputText("##symbolName", ref nameForEdit, 128, ImGuiInputTextFlags.ReadOnly))
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
                if (ImGui.InputText("##symbolChildName", ref nameForEdit, 128))
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

            ImGui.Dummy(new Vector2(0.0f, 5.0f));
        }
        

        private static readonly List<Window> ParameterWindowInstances = new List<Window>();
        private ChangeSymbolNameCommand _symbolNameCommandInFlight;
        private ChangeSymbolNamespaceCommand _symbolNamespaceCommandInFlight;
        private ChangeSymbolChildNameCommand _symbolChildNameCommand;
        private ChangeInputValueCommand _inputValueCommandInFlight;

        private readonly SelectionPinning _pinning = new SelectionPinning();
        private static int _instanceCounter;
    }
}