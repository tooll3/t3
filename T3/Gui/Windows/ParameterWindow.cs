using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Commands;
using T3.Gui.InputUi;
using T3.Gui.Selection;
using T3.Gui.Styling;

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
            
        }

        protected override void DrawAllInstances()
        {
            // Convert to array to allow closing of windowns
            foreach (var w in ParameterWindowInstances.ToArray())
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
                if (instance.Parent == null)
                    return;
                
                var parentUi = SymbolUiRegistry.Entries[instance.Parent.Symbol.Id];
                var symbolChildUi = parentUi.ChildUis.Single(childUi => childUi.Id == instance.SymbolChildId);
                
                DrawSelectedSymbolHeader(instance, symbolChildUi);

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
                    var editState = inputUi.DrawInputEdit(inputSlot, compositionSymbolUi, symbolChildUi);

                    if ((editState & InputEditStateFlags.Started) != InputEditStateFlags.Nothing)
                    {
                        _inputValueCommandInFlight = new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input);
                    }

                    if ((editState & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing)
                    {
                        if (_inputValueCommandInFlight == null || _inputValueCommandInFlight.Value.ValueType != inputSlot.Input.Value.ValueType)
                            _inputValueCommandInFlight = new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input);
                        _inputValueCommandInFlight.Value.Assign(inputSlot.Input.Value);
                    }

                    if ((editState & InputEditStateFlags.Finished) != InputEditStateFlags.Nothing)
                    {
                        if (_inputValueCommandInFlight != null && _inputValueCommandInFlight.Value.ValueType == inputSlot.Input.Value.ValueType)
                            UndoRedoStack.Add(_inputValueCommandInFlight);
                    }

                    if (editState == InputEditStateFlags.ShowOptions)
                    {
                        SelectionManager.SetSelection(inputUi);
                    }

                    ImGui.PopID();
                }

                return;
            }

            if (!SelectionManager.IsAnythingSelected())
                return;

            foreach (var input in SelectionManager.GetSelectedNodes<IInputUi>())
            {
                ImGui.PushID(input.Id.GetHashCode());
                ImGui.PushFont(Fonts.FontLarge);
                ImGui.Text(input.InputDefinition.Name);
                ImGui.PopFont();
                input.DrawSettings();
                ImGui.Spacing();
                ImGui.PopID();
            }
        }

        private void DrawSelectedSymbolHeader(Instance op, SymbolChildUi symbolChildUi)
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
                
                var nameForEdit = symbolChildUi.SymbolChild.Name;
                if (ImGui.InputText("##symbolChildName", ref nameForEdit, 128))
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

            ImGui.Dummy(new Vector2(0.0f, 5.0f));
        }

        private static readonly List<Window> ParameterWindowInstances = new List<Window>();
        private ChangeSymbolNameCommand _symbolNameCommandInFlight;
        private ChangeSymbolNamespaceCommand _symbolNamespaceCommandInFlight;
        private ChangeSymbolChildNameCommand _symbolChildNameCommand;
        private ChangeInputValueCommand _inputValueCommandInFlight;
        private static int _instanceCounter;
    }
}