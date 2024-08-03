using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Model;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.SystemUi;
using T3.Editor.Gui.Commands;
using T3.Editor.Gui.Commands.Graph;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Dialogs;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Gui.Windows.Layouts;
using T3.Editor.UiModel;
using T3.SystemUi;

namespace T3.Editor.Gui.Windows;

internal class ParameterWindow : Window
{
    public ParameterWindow()
    {
        _instanceCounter++;
        Config.Title = LayoutHandling.ParametersPrefix + _instanceCounter;
        AllowMultipleInstances = true;
        Config.Visible = true;
        MenuTitle = "Open New Parameter";
        WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        _parameterWindowInstances.Add(this);
    }

    public override List<Window> GetInstances()
    {
        return _parameterWindowInstances;
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
            var tmpBuffer = "";
            ImGui.PushStyleColor(ImGuiCol.FrameBg, Color.Transparent.Rgba);
            ImGui.InputText("##imgui workaround", ref tmpBuffer, 1);
            ImGui.PopStyleColor();
            ImGui.SameLine();
        }
        
        var graphWindow = GraphWindow.Focused;
        if(graphWindow == null)
            return;

        var nodeSelection = graphWindow.GraphCanvas.NodeSelection;
        if (DrawSettingsForSelectedAnnotations(nodeSelection))
            return;
        
        var instance = nodeSelection.GetSelectedInstanceWithoutComposition();

        var id = instance?.SymbolChildId ?? Guid.Empty;

        if (id != _lastSelectedInstanceId)
        {
            _lastSelectedInstanceId = id;
            _viewMode = ViewModes.Parameters;
        }

        if (instance == null)
        {
            var selectedInputs = nodeSelection.GetSelectedNodes<IInputUi>().ToList();
            if (selectedInputs.Count > 0)
            {
                instance = graphWindow.CompositionOp;
                if(instance == null)
                    return;
                
                var inputUi = selectedInputs.First();
                _viewMode = ViewModes.Settings;
                _parameterSettings.SelectInput(inputUi.Id);
            }
        }
        
        if (instance == null)
            return;

        
        // Draw dialogs
        OperatorHelp.EditDescriptionDialog.Draw(instance.Symbol); // TODO: This is probably not required...
        RenameInputDialog.Draw();

        if (!TryGetUiDefinitions(instance, out var symbolUi, out var symbolChildUi))
            return;

        var modified = false;
        modified |= DrawSymbolHeader(instance, symbolChildUi, symbolUi);

        if (instance.Parent == null)
        {
            CustomComponents.EmptyWindowMessage("Home canvas.");
            return;
        }
        
        switch (_viewMode)
        {
            case ViewModes.Parameters:
                DrawParametersArea(instance, symbolChildUi, symbolUi);
                break;
            case ViewModes.Settings:
                modified |= _parameterSettings.DrawContent(symbolUi, nodeSelection);
                break;
            case ViewModes.Help:
                using (new ChildWindowScope("help",Vector2.Zero, ImGuiWindowFlags.None, Color.Transparent, 0, 0))
                {
                    OperatorHelp.DrawHelp(symbolUi);
                }
                break;
        }
        
        if (modified)
            symbolUi.FlagAsModified();
    }
    
    private bool DrawSymbolHeader(Instance op, SymbolUi.Child symbolChildUi, SymbolUi symbolUi)
    {
        var modified = false;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        ImGui.BeginChild("header", new Vector2(0, ImGui.GetFrameHeight() + 5),
                         false,
                         ImGuiWindowFlags.AlwaysAutoResize
                         | ImGuiWindowFlags.NoScrollbar
                         | ImGuiWindowFlags.NoScrollWithMouse
                         | ImGuiWindowFlags.NoBackground
                         | ImGuiWindowFlags.AlwaysUseWindowPadding);
        {
            ImGui.AlignTextToFramePadding();
            // Namespace and symbol
            ImGui.PushFont(Fonts.FontBold);

            ImGui.TextUnformatted(op.Symbol.Name);
            ImGui.PopFont();

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextDisabled.Rgba);
            ImGui.TextUnformatted(" in ");
            ImGui.PopStyleColor();

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, new Color(0.5f).Rgba);
            var namespaceForEdit = op.Symbol.Namespace ?? "";
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 60); // the question mark is now aligned to the right

            var symbol = op.Symbol;
            var package = symbol.SymbolPackage;
            if (!package.IsReadOnly)
            {
                var args = new InputWithTypeAheadSearch.Args<string>(Label: "##namespace",
                                                                     Items: EditorSymbolPackage.AllSymbols
                                                                                               .Select(i => i.Namespace)
                                                                                               .Distinct()
                                                                                               .OrderBy(i => i),
                                                                     GetTextInfo: i => new InputWithTypeAheadSearch.Texts(i, i, null),
                                                                     Warning: false);
                
                bool namespaceModified = InputWithTypeAheadSearch.Draw(args, ref namespaceForEdit,out _);
                if (namespaceModified && ImGui.IsKeyPressed((ImGuiKey)Key.Return))
                {
                    if (!EditableSymbolProject.ChangeSymbolNamespace(symbol, namespaceForEdit, out var reason))
                    {
                        BlockingWindow.Instance.ShowMessageBox(reason, "Could not rename namespace");
                    }
                }
            }
            else
            {
                ImGui.Text(op.Symbol.Namespace);
            }

            ImGui.PopStyleColor();


            // Settings Modes
            {
                var isSettingsMode = _viewMode == ViewModes.Settings;
                if (_parameterSettings.DrawToggleIcon(symbolUi, ref isSettingsMode))
                {
                    _viewMode = isSettingsMode ? ViewModes.Settings : ViewModes.Parameters;
                }
                CustomComponents.TooltipForLastItem("Click to toggle parameter settings.");
            }

            // Help-Mode
            {
                var isHelpMode = _viewMode == ViewModes.Help;
                if (_help.DrawHelpIcon(symbolUi, ref isHelpMode))
                {
                    _viewMode = isHelpMode ? ViewModes.Help : ViewModes.Parameters;
                }
            }
        }
        ImGui.EndChild();
        ImGui.PopStyleVar();
        return modified;
    }

    private void DrawParametersArea(Instance instance, SymbolUi.Child symbolChildUi, SymbolUi symbolUi)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(5, 5));
        ImGui.BeginChild("parameters", Vector2.Zero, false, ImGuiWindowFlags.AlwaysUseWindowPadding);

        DrawChildNameAndFlags(instance, symbolChildUi, symbolUi);

        var selectedChildSymbolUi = instance.GetSymbolUi();
        var compositionSymbolUi = instance.Parent.GetSymbolUi();

        // Draw parameters
        DrawParameters(instance, selectedChildSymbolUi, symbolChildUi, compositionSymbolUi, false, this );
        FormInputs.AddVerticalSpace(15);

        _help.DrawHelpSummary(symbolUi);

        OperatorHelp.DrawExamples(symbolUi);
        ImGui.EndChild();
        ImGui.PopStyleVar();
    }

    private void DrawChildNameAndFlags(Instance op, SymbolUi.Child symbolChildUi_, SymbolUi symbolUi)
    {
        var hideParameters = _help.IsActive || _parameterSettings.IsActive;
        if (hideParameters)
            return;

        var symbolChildUi = op.GetChildUi();
        
        // SymbolChild Name
        if (symbolChildUi != null)
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 180); //we close the gap after Bypass button 

            var nameForEdit = symbolChildUi.SymbolChild.Name;

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5);
            if (ImGui.InputText("##symbolChildName", ref nameForEdit, 128))
            {
                _symbolChildNameCommand.NewName = nameForEdit;
                symbolChildUi.SymbolChild.Name = nameForEdit;
            }

            ImGui.PopStyleVar();

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
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + new Vector2(5, 4),
                                                  UiColors.TextMuted.Fade(0.5f),
                                                  "Untitled instance");

            ImGui.PushStyleColor(ImGuiCol.Button, UiColors.BackgroundInputField.Rgba);
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
            ImGui.PopStyleColor();
        }

        FormInputs.AddVerticalSpace(5);
    }

    /// <summary>
    /// Draw all parameters of the selected instance.
    /// The actual implementation is done in <see cref="InputValueUi{T}.DrawParameterEdit"/>  
    /// </summary>
    public static void DrawParameters(Instance instance, SymbolUi symbolUi, SymbolUi.Child symbolChildUi,
                                      SymbolUi compositionSymbolUi, bool hideNonEssentials, ParameterWindow parameterWindow = null)
    {
        var groupState = GroupState.None;

        ImGui.PushStyleColor(ImGuiCol.FrameBg, UiColors.BackgroundButton.Rgba);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, UiColors.BackgroundHover.Rgba);
        foreach (var inputSlot in instance.Inputs)
        {
            if (!symbolUi.InputUis.TryGetValue(inputSlot.Id, out IInputUi inputUi))
            {
                Log.Warning("Trying to access an non existing input, probably the op instance is not the actual one.");
                continue;
            }

            InsertGroupsAndPadding(inputUi, ref groupState);

            ImGui.PushID(inputSlot.Id.GetHashCode());
            var skipIfDefault = groupState == GroupState.InsideClosed;

            // Draw the actual parameter line implemented
            // in the generic InputValueUi<T>.DrawParameterEdit() method
            var editState = inputUi.DrawParameterEdit(inputSlot, compositionSymbolUi, symbolChildUi, hideNonEssentials: hideNonEssentials, skipIfDefault);

            // ... and handle the edit state
            if (editState.HasFlag(InputEditStateFlags.Started))
            {
                _inputSlotForActiveCommand = inputSlot;
                _inputValueCommandInFlight =
                    new ChangeInputValueCommand(instance.Parent.Symbol, instance.SymbolChildId, inputSlot.Input, inputSlot.Input.Value);
            }

            if (editState.HasFlag(InputEditStateFlags.Modified))
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

            if (editState.HasFlag(InputEditStateFlags.Finished))
            {
                if (_inputValueCommandInFlight != null && _inputSlotForActiveCommand == inputSlot)
                {
                    UndoRedoStack.Add(_inputValueCommandInFlight);
                }

                _inputValueCommandInFlight = null;
            }

            if (editState == InputEditStateFlags.ShowOptions && parameterWindow != null)
            {
                parameterWindow._viewMode = ViewModes.Settings;
                parameterWindow._parameterSettings.SelectedInputId = inputUi.Id;
                //NodeSelection.SetSelection(inputUi);
            }

            ImGui.PopID();
        }

        ImGui.PopStyleColor(2);

        if (groupState == GroupState.InsideOpened)
            FormInputs.EndGroup();
    }

    private static void InsertGroupsAndPadding(IInputUi inputUi, ref GroupState groupState)
    {
        // Layouts padding and groups
        if (inputUi.AddPadding)
            FormInputs.AddVerticalSpace(2);

        if (string.IsNullOrEmpty(inputUi.GroupTitle))
            return;

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

    private static bool TryGetUiDefinitions(Instance instance, out SymbolUi symbolUi, out SymbolUi.Child symbolChildUi)
    {
        symbolUi = instance.GetSymbolUi();
        
        symbolChildUi = null;
        if (instance.Parent == null)
            return true;

        var parentUi = instance.Parent.GetSymbolUi();
        symbolChildUi = parentUi.ChildUis[instance.SymbolChildId];
        if (symbolChildUi != null)
            return true;

        Log.Warning("Can't find UI definition for symbol " + instance.SymbolChildId);
        return false;
    }
    
    // TODO: Refactor this into a separate class
    private static bool DrawSettingsForSelectedAnnotations(NodeSelection nodeSelection)
    {
        var somethingVisible = false;
        // Draw Annotation settings
        foreach (var annotation in nodeSelection.GetSelectedNodes<Annotation>())
        {
            ImGui.PushID(annotation.Id.GetHashCode());
            ImGui.PushFont(Fonts.FontLarge);
            ImGui.TextUnformatted("Annotation settings" + annotation.Title);
            ImGui.PopFont();

            ImGui.ColorEdit4("color", ref annotation.Color.Rgba);
            ImGui.PopID();
            somethingVisible = true;
        }

        return somethingVisible;
    }
    
    private enum GroupState
    {
        None,
        InsideClosed,
        InsideOpened,
    }

    private enum ViewModes
    {
        Parameters,
        Settings,
        Help,
    }

    private static readonly List<Icon> _modeIcons = new()
                                                        {
                                                            Icon.List,
                                                            Icon.Settings2,
                                                            Icon.HelpOutline
                                                        };

    private ViewModes _viewMode = ViewModes.Parameters;

    public static bool IsAnyInstanceVisible()
    {
        return WindowManager.IsAnyInstanceVisible<ParameterWindow>();
    }

    private static readonly List<Window> _parameterWindowInstances = new();
    private ChangeSymbolChildNameCommand _symbolChildNameCommand;
    private static ChangeInputValueCommand _inputValueCommandInFlight;
    private static IInputSlot _inputSlotForActiveCommand;
    private static int _instanceCounter;
    private Guid _lastSelectedInstanceId;


    private readonly OperatorHelp _help = new();
    private readonly ParameterSettings _parameterSettings = new();
    public static readonly RenameInputDialog RenameInputDialog = new();
}