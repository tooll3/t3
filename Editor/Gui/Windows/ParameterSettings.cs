using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.InputUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Windows;

public class ParameterSettings
{
    public void DrawSettingsIcon(SymbolUi symbolUi)
    {
        // Help indicator
        {
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
            }

            ImGui.PopStyleColor();
        }
    }

    public  void DrawSettings(SymbolUi symbolUi)
    {
        IInputUi selectedInputUi = null;

        
        using (new ChildWindowScope("wrapper", 
                                    new Vector2(0, 0),
                                    ImGuiWindowFlags.None,
                                    UiColors.BackgroundInputField))
        {
            FormInputs.AddVerticalSpace(5);
            
            using (new ChildWindowScope("selector", 
                                         new Vector2(150 * T3Ui.UiScaleFactor, 0),
                                        ImGuiWindowFlags.NoBackground,
                                        Color.Transparent, 
                                        0))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0, 0.5f));
                foreach (var inputUi in symbolUi.InputUis.Values)
                {
                    var isSelected = inputUi.InputDefinition.Id == _selectedInputId;
                    if(isSelected)
                        selectedInputUi = inputUi;
                    
                    var typeColor = TypeUiRegistry.GetPropertiesForType(inputUi.Type).Color;
                    var textColor = isSelected ? UiColors.ForegroundFull : typeColor.Fade(0.9f);
                    var buttonBackground = isSelected ? UiColors.WindowBackground : Color.Transparent;
                    var buttonHoverBackground = isSelected ? typeColor : typeColor.Fade(0.1f);
                        
                    using (new StyleScope( new []
                                               {
                                                   new Coloring(ImGuiCol.Button, buttonBackground),
                                                   new Coloring(ImGuiCol.ButtonActive, buttonBackground),
                                                   new Coloring(ImGuiCol.ButtonHovered, buttonHoverBackground),
                                                   new Coloring(ImGuiCol.Text, textColor),
                                               }))
                    {
                        if (!CustomComponents.RoundedButton(inputUi.InputDefinition.Name, inputUi.InputDefinition.Name, new Vector2(150, ImGui.GetFrameHeight()), ImDrawFlags.RoundCornersLeft))
                            continue;
                        
                        _selectedInputId = inputUi.InputDefinition.Id;
                    }
                }
                
                ImGui.PopStyleVar();
            }

            ImGui.SameLine(0,0);
            
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
                    
                    var modified = false;
            
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
                        if (FormInputs.AddStringInput(null, ref groupTitle, "Group Title", null,
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
                    
                    if(modified)
                        selectedInputUi.Parent.FlagAsModified();
                }
                else
                {
                    CustomComponents.EmptyWindowMessage("no input selected...");
                }
            }
        }
    }
    private enum ParameterListStyles
    {
        Default,
        WithPadding,
        WithGroup,
    }

    
    private Guid _selectedInputId;
    
    public bool IsActive { get; private set; } = false;
}

