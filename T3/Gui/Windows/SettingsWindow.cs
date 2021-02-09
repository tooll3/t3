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
        public static bool UseVSync => _vSync;

        private static bool _vSync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;
        public static float LimitArcConnectionRadius = 100;
        public static float GizmoSize = 100f;

        protected override void DrawContent()
        {
            T3Metrics.Draw();

            ImGui.Separator();
            ImGui.Checkbox("Show Graph thumbnails", ref UserSettings.Config.ShowThumbnails);
            ImGui.Checkbox("Drag snapped nodes", ref UserSettings.Config.SmartGroupDragging);

            ImGui.Checkbox("Use arc connections", ref UserSettings.Config.UseArcConnections);
            ImGui.Checkbox("HideTimeline", ref UserSettings.Config.HideUiElementsInGraphWindow);
            ImGui.DragFloat("Gizmo size", ref GizmoSize);
            ImGui.DragFloat("Limit arc connection radius", ref LimitArcConnectionRadius);
            
            ImGui.Checkbox("Use Jog Dial Control", ref UserSettings.Config.UseJogDialControl);

            ImGui.DragFloat("Scroll damping", ref UserSettings.Config.ZoomSpeed);
            
            ImGui.DragFloat("Snap strength", ref UserSettings.Config.SnapStrength);
            ImGui.DragFloat("Tooltip delay", ref UserSettings.Config.TooltipDelay);
            
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

            
            ImGui.Separator();
            ImGui.Text("Debug options...");
            ImGui.Checkbox("VSync", ref _vSync);
            ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
            ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);
            
            ImGui.Text("Options");
            ColorVariations.DrawSettingsUi();
            if (ImGui.TreeNode("Styles"))
            {
                ImGui.DragFloat("Height Connection Zone", ref GraphNode.UsableSlotThickness);
                ImGui.DragFloat2("Label position", ref GraphNode.LabelPos);
                ImGui.DragFloat("Slot Gaps", ref GraphNode.SlotGaps, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Margin Y", ref GraphNode.InputSlotMargin, 0.1f, 0, 10f);
                ImGui.DragFloat("Input Slot Thickness", ref GraphNode.InputSlotThickness, 0.1f, 0, 10f);
                ImGui.DragFloat("Output Slot Margin", ref GraphNode.OutputSlotMargin, 0.1f, 0, 10f);

                ImGui.ColorEdit4("ValueLabelColor", ref T3Style.ValueLabelColor.Rgba);
                ImGui.ColorEdit4("ValueLabelColorHover", ref T3Style.ValueLabelColorHover.Rgba);
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("ImGui Styles"))
                T3Style.DrawUi();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}