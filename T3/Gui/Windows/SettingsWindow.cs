using System.Collections.Generic;
using ImGuiNET;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.TypeColors;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Config.Title = "Settings";
        }

        public static bool UseVSync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;

        protected override void DrawContent()
        {
            T3Metrics.Draw();
            ImGui.Separator();

            if (ImGui.TreeNode("User Interface"))
            {
                ImGui.Checkbox("Use arc connections", ref UserSettings.Config.UseArcConnections);
                ImGui.Checkbox("Use Jog Dial Control", ref UserSettings.Config.UseJogDialControl);
                ImGui.DragFloat("Scroll smoothing", ref UserSettings.Config.ScrollSmoothing);
                ImGui.Checkbox("Show Graph thumbnails", ref UserSettings.Config.ShowThumbnails);
                ImGui.Checkbox("Drag snapped nodes", ref UserSettings.Config.SmartGroupDragging);
                ImGui.Separator();
                ImGui.DragFloat("Snap strength", ref UserSettings.Config.SnapStrength);
                ImGui.DragFloat("Click threshold", ref UserSettings.Config.ClickTreshold);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Additional settings"))
            {
                ImGui.Checkbox("HideTimeline", ref UserSettings.Config.HideUiElementsInGraphWindow);
                ImGui.DragFloat("Gizmo size", ref UserSettings.Config.GizmoSize);
                ImGui.DragFloat("Tooltip delay", ref UserSettings.Config.TooltipDelay);
                ImGui.Checkbox("Save after symbol creating", ref UserSettings.Config.AutoSaveAfterSymbolCreation);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Debug Options"))
            {
                ImGui.Checkbox("VSync", ref UseVSync);
                ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
                ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);

                if (ImGui.TreeNode("Undo Queue"))
                {
                    ImGui.Text("Undo");
                    ImGui.Indent();
                    foreach (var c in UndoRedoStack.UndoStack)
                    {
                        ImGui.Selectable(c.Name);
                    }

                    ImGui.Unindent();
                    ImGui.Spacing();
                    ImGui.Text("Redo");
                    ImGui.Indent();
                    foreach (var c in UndoRedoStack.RedoStack)
                    {
                        ImGui.Selectable(c.Name);
                    }

                    ImGui.Unindent();
                    ImGui.TreePop();
                }
                
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Look (not saved)"))
            {
                ColorVariations.DrawSettingsUi();

                if (ImGui.TreeNode("T3 Ui Style"))
                {
                    ImGui.DragFloat("Height Connection Zone", ref GraphNode.UsableSlotThickness);
                    ImGui.DragFloat2("Label position", ref GraphNode.LabelPos);
                    ImGui.DragFloat("Slot Gaps", ref GraphNode.SlotGaps, 0.1f, 0, 10f);
                    ImGui.DragFloat("Input Slot Margin Y", ref GraphNode.InputSlotMargin, 0.1f, 0, 10f);
                    ImGui.DragFloat("Input Slot Thickness", ref GraphNode.InputSlotThickness, 0.1f, 0, 10f);
                    ImGui.DragFloat("Output Slot Margin", ref GraphNode.OutputSlotMargin, 0.1f, 0, 10f);

                    ImGui.ColorEdit4("ValueLabelColor", ref T3Style.Colors.ValueLabelColor.Rgba);
                    ImGui.ColorEdit4("ValueLabelColorHover", ref T3Style.Colors.ValueLabelColorHover.Rgba);
                }
                
                if (ImGui.TreeNode("T3 Graph colors"))
                {
                    T3Style.DrawUi();
                    ImGui.TreePop();
                }                
                ImGui.TreePop();
            }
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}