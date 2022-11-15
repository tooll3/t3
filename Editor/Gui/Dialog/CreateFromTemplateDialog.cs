using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Editor.Gui;
using T3.Editor.Gui.Graph;
using T3.Editor.Gui.Graph.Interaction;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.Templates;

namespace T3.Editor.Gui.Dialog
{
    public class CreateFromTemplateDialog : ModalDialog
    {
        public CreateFromTemplateDialog()
        {
            DialogSize = new Vector2(1500, 250);
            Padding = 0;
        }

        public void Draw()
        {
            if (BeginDialog("Create"))
            {
                TemplateDefinition selectedTemplate = null;
                ImGui.BeginChild("templates", new Vector2(200, -1));
                {
                    
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4));
                    for (var index = 0; index < TemplateUse.TemplateDefinitions.Count; index++)
                    {
                        ImGui.PushID(index);
                        var template = TemplateUse.TemplateDefinitions[index];
                        var isSelected = index == _selectedTemplateIndex;
                        if (isSelected)
                            selectedTemplate = template;

                        if (ImGui.Selectable(template.Title, isSelected, ImGuiSelectableFlags.None, new Vector2(ImGui.GetContentRegionAvail().X, 45)))
                        {
                            _selectedTemplateIndex = index;
                        }

                        var keepCursor = ImGui.GetCursorPos();
                        ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(10,-30));
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
                ImGui.BeginChild("canvas", new Vector2(-1, -1), false, ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar);
                {
                    var isNewSymbolNameValid = NodeOperations.IsNewSymbolNameValid(_newSymbolName);
                    
                    if (selectedTemplate != null)
                    {
                        ImGui.PushFont(Fonts.FontLarge);
                        ImGui.TextUnformatted($"Create {selectedTemplate.Title}");
                        ImGui.PopFont();
                        ImGui.TextWrapped(selectedTemplate.Documentation);
                    }

                    CustomComponents.DrawStringParameter("Name", ref _newSymbolName);
                    if (!isNewSymbolNameValid)
                    {
                        ImGui.TextColored(Color.Red,"Sorry, Name has to be unique.");
                    }
                    
                    
                    CustomComponents.DrawStringParameter("NameSpace", ref _newNameSpace, " tes teset setse");
                    CustomComponents.DrawStringParameter("Description", ref _newDescription);
                    
                    if (CustomComponents.DisablableButton("Create", isNewSymbolNameValid, enableTriggerWithReturn:false))
                    {
                        TemplateUse.TryToApplyTemplate(selectedTemplate, _newSymbolName, _newNameSpace, _newDescription);
                        ImGui.CloseCurrentPopup();
                    }
                    
                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndChild();
                EndDialogContent();
            }

            EndDialog();
        }

        private string _newSymbolName= "MyNewOp";
        private string _newNameSpace = "test.test";
        private string _newDescription = "";
        
        private int _selectedTemplateIndex;
    }
}

namespace T3.Editor.Templates
{
    public class TemplateDefinition
    {
        public string Title;
        public string Summary;
        public string Documentation;
        public Guid TemplateSymbolId;
    }

    /// <summary>
    /// Handles the creation of symbols from templates 
    /// </summary>
    public static class TemplateUse
    {
        public static readonly List<TemplateDefinition> TemplateDefinitions
            = new()
                  {
                      new TemplateDefinition
                          {
                              Title = "Empty Project",
                              Summary = "Creates a new project and sets up a folder structure for resources.",
                              Documentation =
                                  "This will create a new Symbol with a basic template to get you started. It will also setup a folder structure for project related files like soundtrack or images.",
                              TemplateSymbolId = Guid.Parse("442995fa-3d89-4d6c-b006-77f825f4e3ed"),
                          },
                      new TemplateDefinition
                          {
                              Title = "3d Project",
                              Summary = "Something else",
                              Documentation =
                                  "TDB",
                              TemplateSymbolId = Guid.Parse("442995fa-3d89-4d6c-b006-77f825f4e3ed"),
                          },
                  };

        public static void TryToApplyTemplate(TemplateDefinition template, string newTypeName, string nameSpace, string description)
        {
            var defaultCanvasWindow = GraphWindow.GetPrimaryGraphWindow();
            if (defaultCanvasWindow == null)
            {
                Log.Warning("Can't create from template without open graph window");
                return;
            }
            
            var defaultComposition = GraphWindow.GetMainComposition();
            if (!SymbolUiRegistry.Entries.TryGetValue(defaultComposition.Symbol.Id, out var compositionSymbolUi))
            {
                Log.Warning("Can't find default op");
                return;
            }

            var centerOnScreen = defaultCanvasWindow.GraphCanvas.WindowPos + defaultCanvasWindow.GraphCanvas.WindowSize / 2;
            var positionOnCanvas2 = defaultCanvasWindow.GraphCanvas.InverseTransformPositionFloat(centerOnScreen);
            var freePosition = FindFreePositionOnCanvas(defaultCanvasWindow.GraphCanvas, positionOnCanvas2);
            NodeOperations.DuplicateAsNewType(compositionSymbolUi, template.TemplateSymbolId, newTypeName, nameSpace, description, freePosition);
        }

        private static Vector2 FindFreePositionOnCanvas(GraphCanvas canvas, Vector2 pos)
        {
            
            if(!SymbolUiRegistry.Entries.TryGetValue(canvas.CompositionOp.Symbol.Id, out var symbolUi))
            {
                Log.Error("Can't find symbol child on composition op?");
                return Vector2.Zero;
            }
            while (true)
            {
                var isPositionFree = true;
                foreach (var childUi in symbolUi.ChildUis)
                {
                    var rect = ImRect.RectWithSize(childUi.PosOnCanvas, childUi.Size);
                    rect.Expand(20);
                    if (!rect.Contains(pos))
                        continue;
                    
                    pos.X += childUi.Size.X;
                    isPositionFree = false;
                    break;
                }

                if (isPositionFree)
                    return pos;
            }
        }
    }
}