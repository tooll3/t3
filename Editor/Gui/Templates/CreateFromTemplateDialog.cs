using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.Templates;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Dialog
{
    public class CreateFromTemplateDialog : ModalDialog
    {
        public CreateFromTemplateDialog()
        {
            DialogSize = new Vector2(800, 400);
            Padding = 0;
        }

        public void Draw()
        {
            if (_selectedTemplateIndex == -1)
            {
                _selectedTemplateIndex = 0;
                ApplyTemplateSwitch();
            }
            
            if (BeginDialog("Create"))
            {
                TemplateDefinition selectedTemplate = null;
                ImGui.BeginChild("templates", new Vector2(200, -1));
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4));
                    for (var index = 0; index < TemplateDefinition.TemplateDefinitions.Count; index++)
                    {
                        ImGui.PushID(index);
                        var template = TemplateDefinition.TemplateDefinitions[index];
                        var isSelected = index == _selectedTemplateIndex;
                        if (isSelected)
                            selectedTemplate = template;

                        if (ImGui.Selectable(template.Title, isSelected, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 45)))
                        {
                            _selectedTemplateIndex = index;
                            ApplyTemplateSwitch();
                        }

                        var keepCursor = ImGui.GetCursorPos();
                        ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(10, -30));
                        ImGui.PushFont(Fonts.FontSmall);
                        ImGui.TextWrapped(template.Summary);
                        ImGui.PopFont();
                        ImGui.SetCursorPos(keepCursor);
                        ImGui.PopID();
                    }

                    //DrawSidePanelContent();
                    ImGui.Selectable("An option");
                    ImGui.EndChild();
                    ImGui.PopStyleVar();
                }

                ImGui.SameLine();
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10,10));
                ImGui.BeginChild("options", new Vector2(-1, -1), false, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);
                {
                    if (selectedTemplate != null)
                    {
                        ImGui.PushFont(Fonts.FontLarge);
                        ImGui.TextUnformatted($"Create {selectedTemplate.Title}");
                        ImGui.PopFont();
                        ImGui.TextWrapped(selectedTemplate.Documentation);
                    }

                    var isNewSymbolNameValid = NodeOperations.IsNewSymbolNameValid(_newSymbolName);
                    CustomComponents.DrawStringParameter("Name",
                                                         ref _newSymbolName,
                                                         null,
                                                         isNewSymbolNameValid ? null : "Symbols must not contain spaces or special characters.");

                    var isNamespaceValid = NodeOperations.IsNameSpaceValid(NameSpace);
                    CustomComponents.DrawStringParameter("NameSpace",
                                                         ref _newNameSpace,
                                                         NameSpace,
                                                         isNamespaceValid ? null : "Is required and may only include characters, numbers and dots."
                                                        );

                    
                    var isResourceFolderValid = _validResourceFolderPattern.IsMatch(ResourceDirectory);
                    CustomComponents.DrawStringParameter("Resource Director",
                                                         ref _resourceFolder,
                                                         ResourceDirectory,
                                                         isResourceFolderValid ? null : "Your project files must be in Resources\\ directory for exporting."
                                                        );
                    
                    CustomComponents.DrawStringParameter("Description", ref _newDescription);

                    if (CustomComponents.DisablableButton("Create", 
                                                          isNewSymbolNameValid && isNamespaceValid, 
                                                          enableTriggerWithReturn: false))
                    {
                        TemplateUse.TryToApplyTemplate(selectedTemplate, _newSymbolName, NameSpace, _newDescription, ResourceDirectory);
                        ImGui.CloseCurrentPopup();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
                EndDialogContent();
            }

            EndDialog();
        }

        private void ApplyTemplateSwitch()
        {
            var newTemplate = TemplateDefinition.TemplateDefinitions[_selectedTemplateIndex];
            _newSymbolName = newTemplate.DefaultSymbolName ?? "MyOp";
        }
        
        private static readonly Regex _validResourceFolderPattern = new Regex(@"^Resources\\([A-Za-z][A-Za-z\d]*)(\\([A-Za-z][A-Za-z\d]*))*\\?$");
        
        private string NameSpace => string.IsNullOrEmpty(_newNameSpace) ? $"user.{UserSettings.Config.UserName}.{_newSymbolName}" : _newNameSpace;
        private string ResourceDirectory => string.IsNullOrEmpty(_resourceFolder) ? $"Resources\\user\\{UserSettings.Config.UserName}\\{_newSymbolName}\\" : _resourceFolder;

        private string _newSymbolName = "MyNewOp";
        private string _newNameSpace = null;
        private string _newDescription = null;
        private string _resourceFolder = null;

        private int _selectedTemplateIndex = -1;
    }
}

namespace T3.Editor.Templates
{
}