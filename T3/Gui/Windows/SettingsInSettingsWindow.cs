using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ImGuiNET;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows
{
    public partial class SettingsWindow
    {
        static readonly UIControlledSetting[] userInterfaceSettings = new UIControlledSetting[]
        {
            new UIControlledSetting
            (
                label: "Warn before Lib modifications",
                tooltip: "This warning pops up when you attempt to enter an Operator that ships with the application.\n" +
                         "If unsure, this is best left checked.",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.WarnBeforeLibEdit)
            ),

            new UIControlledSetting
            (
                label: "Use arc connections",
                tooltip: "Affects the shape of the connections between your Operators",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.UseArcConnections)
            ),

            new UIControlledSetting
            (
                label: "Use Jog Dial Control",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.UseJogDialControl)
            ),

            new UIControlledSetting
            (
                label: "Show Graph thumbnails",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.ShowThumbnails)
            ),

            new UIControlledSetting
            (
                label: "Drag snapped nodes",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.SmartGroupDragging)
            ),

            new UIControlledSetting
            (
                label: "Fullscreen Window Swap",
                tooltip: "Swap main and second windows when fullscreen",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen)
            ),

            new UIControlledSetting
            (
                label: "UI Scale",
                tooltip: "The global scale of all rendered UI in the application",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.UiScaleFactor, 0.01f, 3f)
            ),

            new UIControlledSetting
            (
                label: "Scroll smoothing",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.ScrollSmoothing)
            ),

            new UIControlledSetting
            (
                label: "Snap strength",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.SnapStrength)
            ),

            new UIControlledSetting
            (
                label: "Click threshold",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.ClickThreshold)
            ),

            new UIControlledSetting
            (
                label: "Timeline Raster Density",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.TimeRasterDensity)
            ),
        };

        static readonly UIControlledSetting[] spaceMouseSettings = new UIControlledSetting[]
        {
            new UIControlledSetting
            (
                label: "Smoothing",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.SpaceMouseDamping, 0.01f, 1f)
            ),

            new UIControlledSetting
            (
                label: "Move Speed",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.SpaceMouseMoveSpeedFactor, 0, 10f)
            ),

            new UIControlledSetting
            (
                label: "Rotation Speed",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.SpaceMouseRotationSpeedFactor, 0, 10f)
            )
        };

        static readonly UIControlledSetting[] additionalSettings = new UIControlledSetting[]
        {
            new UIControlledSetting
            (
                label: "Gizmo size",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.GizmoSize)
            ),

            new UIControlledSetting
            (
                label: "Tooltip delay",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref UserSettings.Config.TooltipDelay)
            ),


            // These settings were laid out in the old UI, kept here for someone else to ultimately choose to yeet or unyeet them

            //new UIControlledSetting
            //(
            //    label: "Show Title",
            //    guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.ShowTitleAndDescription)
            //),

            //new UIControlledSetting
            //(
            //    label: "Show Timeline",
            //    guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UserSettings.Config.ShowTimeline)
            //)
        };
        
        static readonly UIControlledSetting[] debugSettings = new UIControlledSetting[]
        {
            new UIControlledSetting
            (
                label: "VSync",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref UseVSync)
            ),

            new UIControlledSetting
            (
                label: "Show Window Regions",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref WindowRegionsVisible)
            ),

            new UIControlledSetting
            (
                label: "Show Item Regions",
                guiFunc: (string guiLabel) => ImGui.Checkbox(guiLabel, ref ItemRegionsVisible)
            ),
        };

        static readonly UIControlledSetting[] t3UiStyleSettings = new UIControlledSetting[]
        {
            new UIControlledSetting
            (
                label: "Label position",
                guiFunc: (string guiLabel) => ImGui.DragFloat2(guiLabel, ref GraphNode.LabelPos)
            ),

            new UIControlledSetting
            (
                label: "Height Connection Zone",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref GraphNode.UsableSlotThickness)
            ),

            new UIControlledSetting
            (
                label: "Slot Gaps",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref GraphNode.SlotGaps, 0, 10f)
            ),

            new UIControlledSetting
            (
                label: "Input Slot Margin Y",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref GraphNode.InputSlotMargin, 0, 10f)
            ),

            new UIControlledSetting
            (
                label: "Input Slot Thickness",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref GraphNode.InputSlotThickness, 0, 10f)
            ),

            new UIControlledSetting
            (
                label: "Output Slot Margin",
                guiFunc: (string guiLabel) => CustomComponents.FloatValueEdit(guiLabel, ref GraphNode.OutputSlotMargin, 0, 10f)
            ),

            new UIControlledSetting
            (
                label: "Value Label Color",
                guiFunc: (string guiLabel) => ImGui.ColorEdit4(guiLabel, ref T3Style.Colors.ValueLabelColor.Rgba)
            ),

            new UIControlledSetting
            (
                label: "Value Label Color Hover",
                guiFunc: (string guiLabel) => ImGui.ColorEdit4(guiLabel, ref T3Style.Colors.ValueLabelColorHover.Rgba)
            ),
        };
    }
}