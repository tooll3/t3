using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows;

internal class ParameterWindow : Window
{
    public ParameterWindow()
    {
        _instanceCounter++;
        Config.Title = "Parameters##" + _instanceCounter;
        AllowMultipleInstances = true;
        Config.Visible = true;
        MenuTitle = "Open New Parameter";

        _parameterWindowInstances.Add(this);
    }

    public override List<Window> GetInstances()
    {
        return _parameterWindowInstances;
    }

    protected override void DrawAllInstances()
    {
        // Convert to array to allow closing of windows
        foreach (var w in _parameterWindowInstances.ToArray())
        {
            w.DrawOneInstance();
        }
    }

    protected override void Close()
    {
        _parameterWindowInstances.Remove(this);
    }

    protected override void AddAnotherInstance()
    {
        // ReSharper disable once ObjectCreationAsStatement
        new ParameterWindow(); // Required to call constructor
    }

    protected override void DrawContent()
    {
        // Insert invisible spill over input to catch accidental imgui focus attempts
        {
            ImGui.SetNextItemWidth(2);
            string tmpBuffer = "";
            ImGui.InputText("##imgui workaround", ref tmpBuffer, 1);
            ImGui.SameLine();
        }

        var instance = NodeSelection.GetFirstSelectedInstance();
        if (instance != null)
        {
            if (!SymbolUiRegistry.Entries.TryGetValue(instance.Symbol.Id, out var symbolUi))
            {
                Log.Warning("Can't find UI definition for symbol " + instance.SymbolChildId);
                return;
            }

            OperatorHelp.EditDescriptionDialog.Draw(instance.Symbol);
            RenameInputDialog.Draw();

            SymbolChildUi symbolChildUi = null;
            if (instance.Parent != null)
            {
                var parentUi = SymbolUiRegistry.Entries[instance.Parent.Symbol.Id];
                symbolChildUi = parentUi.ChildUis.SingleOrDefault(childUi => childUi.Id == instance.SymbolChildId);
                if (symbolChildUi == null)
                {
                    Log.Warning("Can't find UI definition for symbol " + instance.SymbolChildId);
                    return;
                }
            }

            if (DrawSelectedSymbolHeader(instance, symbolChildUi, symbolUi))
            {
                symbolUi.FlagAsModified();
            }

            if (instance.Parent != null && !_help.IsActive)
            {
                var selectedChildSymbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
                var compositionSymbolUi = SymbolUiRegistry.Entries[instance.Parent.Symbol.Id];

                // Draw parameters
                DrawParameters(instance, selectedChildSymbolUi, symbolChildUi, compositionSymbolUi, false);
                FormInputs.AddVerticalSpace(15);

                FormInputs.SetIndentToLeft();
                FormInputs.AddHint("Check the documentation in the header");

                OperatorHelp.DrawExamples(symbolUi);
            }
            else
            {
                OperatorHelp.DrawDescription(symbolUi);
            }

            return;
        }

        if (!NodeSelection.IsAnythingSelected())
            return;

        // Draw parameter Settings
        foreach (var inputUi in NodeSelection.GetSelectedNodes<IInputUi>())
        {
            ImGui.PushID(inputUi.Id.GetHashCode());
            ImGui.PushFont(Fonts.FontLarge);
            ImGui.TextUnformatted("Parameter settings for " + inputUi.InputDefinition.Name);
            FormInputs.AddVerticalSpace(5);
            FormInputs.SetIndent(100);
            ImGui.PopFont();
            inputUi.DrawSettings();
            ImGui.Spacing();
            inputUi.DrawDescriptionEdit();
            ImGui.PopID();
        }

        // Draw Annotation settings
        foreach (var annotation in NodeSelection.GetSelectedNodes<Annotation>())
        {
            ImGui.PushID(annotation.Id.GetHashCode());
            ImGui.PushFont(Fonts.FontLarge);
            ImGui.TextUnformatted("Annotation settings" + annotation.Title);
            ImGui.PopFont();

            ImGui.ColorEdit4("color", ref annotation.Color.Rgba);
            ImGui.PopID();
        }
    }

    private enum GroupState
    {
        None,
        InsideClosed,
        InsideOpened,
    }

    private bool DrawSelectedSymbolHeader(Instance op, SymbolChildUi symbolChildUi, SymbolUi symbolUi)
    {
        var modified = false;

        // Namespace and symbol
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.One * 5);

            ImGui.PushFont(Fonts.FontBold);

            ImGui.TextUnformatted(op.Symbol.Name);
            ImGui.PopFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
            ImGui.TextUnformatted(" in ");
            ImGui.PopStyleColor();
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
            var namespaceForEdit = op.Symbol.Namespace ?? "";

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 42);
            if (InputWithTypeAheadSearch.Draw("##namespace", ref namespaceForEdit,
                                              SymbolRegistry.Entries.Values.Select(i => i.Namespace).Distinct().OrderBy(i => i)))
            {
                op.Symbol.Namespace = namespaceForEdit;
                modified = true;
            }

            ImGui.PopStyleColor();
        }

        _help.DrawHelpIcon(symbolUi);

        // SymbolChild Name
        if (symbolChildUi != null)
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + Vector2.One * 5);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 205);

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
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + new Vector2(5, 5),
                                                  UiColors.TextMuted,
                                                  "Untitled instance");

            // Disabled toggle
            {
                ImGui.SameLine();
                ImGui.PushFont(Fonts.FontBold);
                if (symbolChildUi.IsDisabled)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, UiColors.StatusAttention.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);
                    if (ImGui.Button("DISABLED", new Vector2(90, 0)))
                    {
                        UndoRedoStack.AddAndExecute(new ChangeInstanceIsDisabledCommand(symbolChildUi, false));
                    }

                    ImGui.PopStyleColor(2);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                    if (ImGui.Button("ENABLED", new Vector2(90, 0)))
                    {
                        UndoRedoStack.AddAndExecute(new ChangeInstanceIsDisabledCommand(symbolChildUi, true));
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.PopFont();
            }

            // Bypass
            {
                ImGui.SameLine();
                ImGui.PushFont(Fonts.FontBold);
                if (symbolChildUi.SymbolChild.IsBypassed)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, UiColors.StatusAttention.Rgba);
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.Text.Rgba);

                    // TODO: check if bypassable
                    if (ImGui.Button("BYPASSED", new Vector2(90, 0)))
                    {
                        UndoRedoStack.AddAndExecute(new ChangeInstanceBypassedCommand(symbolChildUi.SymbolChild, false));
                    }

                    ImGui.PopStyleColor(2);
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                    if (ImGui.Button("BYPASS", new Vector2(90, 0)))
                    {
                        UndoRedoStack.AddAndExecute(new ChangeInstanceBypassedCommand(symbolChildUi.SymbolChild, true));
                    }

                    ImGui.PopStyleColor();
                }

                ImGui.PopFont();
            }
        }

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        return modified;
    }

    /// <summary>
    /// Draw all parameters of the selected instance.
    /// The actual implementation is done in <see cref="InputValueUi{T}.DrawParameterEdit"/>  
    /// </summary>
    public static void DrawParameters(Instance instance, SymbolUi symbolUi, SymbolChildUi symbolChildUi,
                                      SymbolUi compositionSymbolUi, bool hideNonEssentials)
    {
        var groupState = GroupState.None;

        foreach (var inputSlot in instance.Inputs)
        {
            if (!symbolUi.InputUis.TryGetValue(inputSlot.Id, out IInputUi inputUi))
            {
                Log.Warning("Trying to access an non existing input, probably the op instance is not the actual one.");
                continue;
            }

            if (inputUi.AddPadding)
                FormInputs.AddVerticalSpace(2);

            if (!string.IsNullOrEmpty(inputUi.GroupTitle))
            {
                if (groupState == GroupState.InsideOpened)
                    FormInputs.EndGroup();

                if (inputUi.GroupTitle.EndsWith("..."))
                {
                    var isOpen = FormInputs.BeginGroup(inputUi.GroupTitle);
                    groupState = isOpen ? GroupState.InsideOpened : GroupState.InsideClosed;
                }
                else
                {
                    groupState = GroupState.None;
                    FormInputs.AddVerticalSpace(5);
                    ImGui.PushFont(Fonts.FontSmall);
                    ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                    ImGui.SetCursorPosX(4);
                    ImGui.TextUnformatted(inputUi.GroupTitle.ToUpperInvariant());
                    ImGui.PopStyleColor();
                    ImGui.PopFont();
                    FormInputs.AddVerticalSpace(2);
                }
            }

            ImGui.PushID(inputSlot.Id.GetHashCode());
            var skipIfDefault = groupState == GroupState.InsideClosed;
            var editState = inputUi.DrawParameterEdit(inputSlot, compositionSymbolUi, symbolChildUi, hideNonEssentials: hideNonEssentials, skipIfDefault);

            if ((editState & InputEditStateFlags.Started) != InputEditStateFlags.Nothing)
            {
                _inputSlotForActiveCommand = inputSlot;
                _inputValueCommandInFlight =
                    new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input, inputSlot.Input.Value);
            }

            if ((editState & InputEditStateFlags.Modified) != InputEditStateFlags.Nothing)
            {
                if (_inputValueCommandInFlight == null || _inputSlotForActiveCommand != inputSlot)
                {
                    _inputValueCommandInFlight =
                        new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input, inputSlot.Input.Value);
                    _inputSlotForActiveCommand = inputSlot;
                }

                _inputValueCommandInFlight.AssignNewValue(inputSlot.Input.Value);
                inputSlot.DirtyFlag.Invalidate();
            }

            if ((editState & InputEditStateFlags.Finished) != InputEditStateFlags.Nothing)
            {
                if (_inputValueCommandInFlight != null && _inputSlotForActiveCommand == inputSlot)
                {
                    UndoRedoStack.Add(_inputValueCommandInFlight);
                }

                _inputValueCommandInFlight = null;
            }

            if (editState == InputEditStateFlags.ShowOptions)
            {
                NodeSelection.SetSelection(inputUi);
            }

            ImGui.PopID();
        }

        if (groupState == GroupState.InsideOpened)
            FormInputs.EndGroup();
    }

    public static bool IsAnyInstanceVisible()
    {
        return WindowManager.IsAnyInstanceVisible<ParameterWindow>();
    }

    private static readonly List<Window> _parameterWindowInstances = new();
    private ChangeSymbolChildNameCommand _symbolChildNameCommand;
    private static ChangeInputValueCommand _inputValueCommandInFlight;
    private static IInputSlot _inputSlotForActiveCommand;
    private static int _instanceCounter;
    private OperatorHelp _help = new();
    public static readonly RenameInputDialog RenameInputDialog = new();
}