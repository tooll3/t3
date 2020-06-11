using System;
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
                DrawParameters(instance, selectedChildSymbolUi, symbolChildUi, compositionSymbolUi);

                ImGui.PushFont(Fonts.FontSmall);
                var symbolUi= SymbolUiRegistry.Entries[instance.Symbol.Id];
                if (!string.IsNullOrEmpty(symbolUi.Description))
                {
                    ImGui.Text(symbolUi.Description);
                }
                ImGui.PopFont();
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

        public static void DrawParameters(Instance instance, SymbolUi symbolUi, SymbolChildUi symbolChildUi, 
                                           SymbolUi compositionSymbolUi)
        {
            foreach (var inputSlot in instance.Inputs)
            {
                if (!symbolUi.InputUis.TryGetValue(inputSlot.Id, out IInputUi inputUi))
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
        }

        private void DrawSelectedSymbolHeader(Instance op, SymbolChildUi symbolChildUi)
        {
            // namespace and symbol
            {
                ImGui.SetCursorPos( ImGui.GetCursorPos()+ Vector2.One * 5);
                ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
                ImGui.Text(op.Symbol.Namespace ?? "");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.Dummy(new Vector2(10,0));
                ImGui.SameLine();
                ImGui.Text(op.Symbol.Name);
                ImGui.Dummy(Vector2.One * 5);
            }

            // SymbolChild Name
            {
                ImGui.PushFont(Fonts.FontLarge);
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

                // Fake placeholder text
                if (string.IsNullOrEmpty(nameForEdit))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 6));
                    ImGui.SetCursorScreenPos(ImGui.GetItemRectMin());
                    ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.2f).Rgba);
                    ImGui.SetCursorPos( ImGui.GetCursorPos()+ Vector2.One * 5);
                    ImGui.Text("Untitled instance");
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar();
                }

                ImGui.PopFont();
            }

            ImGui.Dummy(new Vector2(0.0f, 5.0f));
        }

        public static bool IsAnyInstanceVisible()
        {
            return T3Ui.WindowManager.IsAnyInstanceVisible<ParameterWindow>();
        }
        
        private static readonly List<Window> ParameterWindowInstances = new List<Window>();
        private ChangeSymbolChildNameCommand _symbolChildNameCommand;
        private static ChangeInputValueCommand _inputValueCommandInFlight;
        private static int _instanceCounter;
    }
}