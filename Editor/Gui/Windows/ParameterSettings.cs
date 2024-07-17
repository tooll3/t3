using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Graph.Modification;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows;

public class ParameterSettings
{
    public bool DrawToggleIcon(SymbolUi symbolUi, ref bool isEnabled)
    {
        var modified = false;
        
        IsActive = isEnabled;
        ImGui.SameLine();
        var w = ImGui.GetFrameHeight();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 2);
        ImGui.PushStyleColor(ImGuiCol.Button, Color.Transparent.Rgba);
        if (CustomComponents.IconButton(
                                        Icon.List,
                                        new Vector2(w, w),
                                        IsActive
                                            ? CustomComponents.ButtonStates.Activated
                                            : CustomComponents.ButtonStates.Dimmed
                                       ))
        {
            IsActive = !IsActive;
            modified = true;
            isEnabled = IsActive;
        }

        ImGui.PopStyleColor();
        return modified;
    }
    
    public bool DrawContent(SymbolUi symbolUi)
    {
        var modified = false;
        
        using (new ChildWindowScope("wrapper", 
                                    new Vector2(0, 0),
                                    ImGuiWindowFlags.None,
                                    UiColors.BackgroundInputField))
        {
            FormInputs.AddVerticalSpace(5);
            
            var selectedInputUi = DrawSelection(symbolUi);
            ImGui.SameLine(0,0);
            modified = DrawSettingsForParameter(selectedInputUi);
        }

        return modified;
    }

    private float _selectionWidth = 150;
    private IInputUi DrawSelection(SymbolUi symbolUi)
    {
        IInputUi selectedInputUi = null;
        
        using (new ChildWindowScope("selector", 
                                    new Vector2(_selectionWidth * T3Ui.UiScaleFactor, 0),
                                    ImGuiWindowFlags.NoBackground,
                                    Color.Transparent, 
                                    0))
        {
            var dl = ImGui.GetWindowDrawList();
            var parentSymbol = symbolUi.Symbol;

            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
                
            for (var index = 0; index < symbolUi.InputUis.Values.ToList().Count; index++)
            {
                var inputUi = symbolUi.InputUis.Values.ToList()[index];
                var isSelected = inputUi.InputDefinition.Id == _selectedInputId;
                if (isSelected)
                    selectedInputUi = inputUi;

                var padding = inputUi.AddPadding ? 4 : 0f;
                if (!string.IsNullOrEmpty(inputUi.GroupTitle))
                    padding = 13;

                var size = new Vector2(_selectionWidth * T3Ui.UiScaleFactor, padding + ImGui.GetFrameHeight());
                var clicked = ImGui.InvisibleButton(inputUi.InputDefinition.Name, size);

                var typeColor = TypeUiRegistry.GetPropertiesForType(inputUi.Type).Color;
                var textColor = isSelected ? UiColors.ForegroundFull : typeColor.Fade(0.9f);
                var backgroundColor = isSelected ? UiColors.WindowBackground : Color.Transparent;
                if (ImGui.IsItemHovered() && !isSelected)
                {
                    backgroundColor = UiColors.WindowBackground.Fade(0.5f);
                }

                // Handle dragging
                if (ImGui.IsItemActive() && !ImGui.IsItemHovered())
                {
                    var mouseDelta = ImGui.GetMouseDragDelta().Y;

                    var indexDelta = mouseDelta switch
                                         {
                                             < 0 when ImGui.GetItemRectMin().Y > ImGui.GetMousePos().Y && index > 0                           => -1,
                                             > 0 when ImGui.GetItemRectMin().Y < ImGui.GetMousePos().Y && index < symbolUi.InputUis.Count - 1 => 1,
                                             _                                                                                                => 0
                                         };

                    if (indexDelta != 0)
                    {
                        (parentSymbol.InputDefinitions[index + indexDelta], parentSymbol.InputDefinitions[index]) 
                            = (parentSymbol.InputDefinitions[index], parentSymbol.InputDefinitions[index + indexDelta]);
                            
                        (symbolUi.InputUis[index + indexDelta], symbolUi.InputUis[index]) 
                            = (symbolUi.InputUis[index], symbolUi.InputUis[index + indexDelta]);
                            
                        _wasDraggingParameterOrder = true;
                    }
                }

                dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), backgroundColor, 7, ImDrawFlags.RoundCornersLeft);

                // Draw group title
                if (!string.IsNullOrEmpty(inputUi.GroupTitle))
                {
                    dl.AddText(Fonts.FontSmall, Fonts.FontSmall.FontSize,
                               ImGui.GetItemRectMin()
                               + new Vector2(6, 0),
                               UiColors.TextMuted.Fade(0.5f),
                               inputUi.GroupTitle);
                }

                // Draw name
                dl.AddText(Fonts.FontNormal, Fonts.FontNormal.FontSize,
                           ImGui.GetItemRectMin()
                           + new Vector2(6, padding + 2),
                           textColor,
                           inputUi.InputDefinition.Name);

                if (clicked && !_wasDraggingParameterOrder)
                { 
                    _selectedInputId = inputUi.InputDefinition.Id;
                    selectedInputUi = inputUi;
                    var selectedInstance = NodeSelection.GetFirstSelectedInstance();
                    if(selectedInstance == null) 
                        NodeSelection.SetSelection(inputUi);
                }
            }

            ImGui.PopStyleVar();

            if (selectedInputUi == null && symbolUi.InputUis.Count > 0)
            {
                _selectedInputId = symbolUi.InputUis.Values.First().InputDefinition.Id;
            }
            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left) && _wasDraggingParameterOrder)
            {
                _wasDraggingParameterOrder = false;
                parentSymbol.SortInputSlotsByDefinitionOrder();
                InputsAndOutputs.AdjustInputOrderOfSymbol(parentSymbol);
                Graph.Graph.RequestUpdate();
                Log.Debug(" Applying new parameter order" + ImGui.GetMouseDragDelta().Y);
            }
        }

        return selectedInputUi;
    }

    private static bool DrawSettingsForParameter(IInputUi selectedInputUi)
    {
        var modified = false;
        using (new ChildWindowScope("settings", 
                                    new Vector2(0,0), ImGuiWindowFlags.None,
                                    UiColors.WindowBackground.Rgba, 
                                    10, 
                                    5))
        {            
            if (selectedInputUi != null)
            {
                FormInputs.AddSectionHeader(selectedInputUi.InputDefinition.Name);
                ImGui.PushStyleColor(ImGuiCol.Text, UiColors.TextMuted.Rgba);
                ImGui.TextUnformatted( selectedInputUi.Type.Name);
                ImGui.PopStyleColor();
                FormInputs.AddVerticalSpace(5);
                    
                var style = ParameterListStyles.Default;
                if (selectedInputUi.AddPadding)
                {
                    style = ParameterListStyles.WithPadding;
                }
                else if (selectedInputUi.GroupTitle != null)
                {
                    style = ParameterListStyles.WithGroup;
                }
            
                FormInputs.DrawFieldSetHeader("Show parameter as");
                if (FormInputs.AddEnumDropdown(ref style, null))
                {
                    switch (style)
                    {
                        case ParameterListStyles.Default:
                            selectedInputUi.AddPadding = false;
                            selectedInputUi.GroupTitle = null;
                            break;
                        case ParameterListStyles.WithPadding:
                            selectedInputUi.AddPadding = true;
                            selectedInputUi.GroupTitle = null;
                            break;
                        case ParameterListStyles.WithGroup:
                            selectedInputUi.AddPadding = false;
                            selectedInputUi.GroupTitle = "";
                            break;
                    }
                    modified = true;
                }
            
                if (style == ParameterListStyles.WithGroup)
                {
                    var groupTitle = selectedInputUi.GroupTitle;
                    if (FormInputs.AddStringInput("##groupTitle", ref groupTitle, "Group Title", null,
                                                  "Group title shown above parameter\n\nGroup will be collapsed by default if name ends with '...' (three dots)."))
                    {
                        selectedInputUi.GroupTitle = groupTitle;
                        modified = true;
                    }
                }
            
                FormInputs.DrawFieldSetHeader("Relevancy in Graph");
                var tmpForRef = selectedInputUi.Relevancy;
                if (FormInputs.AddEnumDropdown(ref tmpForRef, null, defaultValue: Relevancy.Optional))
                {
                    selectedInputUi.Relevancy = tmpForRef;
                    modified = true;
                }
                    
                // Draw additional settings for input types like Vector2, Vector3, etc.
                selectedInputUi.DrawSettings();
                    
                FormInputs.AddVerticalSpace();

                FormInputs.DrawFieldSetHeader("Documentation");
                var width = ImGui.GetContentRegionAvail().X;
                var description = string.IsNullOrEmpty(selectedInputUi.Description) ? string.Empty : selectedInputUi.Description;
                    
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,5);
                if (ImGui.InputTextMultiline("##parameterDescription", ref description, 16000, new Vector2(width, 0)))
                {
                    selectedInputUi.Description = string.IsNullOrEmpty(description) ? null : description;
                    modified = true;
                }
                ImGui.PopStyleVar();
            }
            else
            {
                CustomComponents.EmptyWindowMessage("no input selected...");
            }
        }

        return modified;
    }

    private enum ParameterListStyles
    {
        Default,
        WithPadding,
        WithGroup,
    }

    private static bool _wasDraggingParameterOrder;
    private Guid _selectedInputId;
    
    public bool IsActive { get; private set; } = false;

    public void SelectInput(Guid inputUiId)
    {
        _selectedInputId = inputUiId;
    }
}

