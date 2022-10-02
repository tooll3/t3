using System;
using ImGuiNET;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows
{
    public partial class SettingsWindow
    {
        class UIControlledSetting
        {
            public string label;
            public string tooltip;
            public Func<bool> imguiFunc;
            public Action OnValueChanged;
        }

        static readonly UIControlledSetting[] userInterfaceSettings = new UIControlledSetting[]
        {
            new UIControlledSetting()
            {
                label = "UI Scale",
                imguiFunc = () => ImGui.DragFloat("##UiScaleFactor", ref UserSettings.Config.UiScaleFactor, 0.01f, 0.5f, 3f)
            },

            new UIControlledSetting()
            {
                label = "Warn before Lib modifications",
                imguiFunc = () => ImGui.Checkbox("##WarnBeforeLibEdit", ref UserSettings.Config.WarnBeforeLibEdit)
            },

            new UIControlledSetting()
            {
                label = "Use arc connections",
                imguiFunc = () => ImGui.Checkbox("##UseArcConnections", ref UserSettings.Config.UseArcConnections)
            },

            new UIControlledSetting()
            {
                label = "Use Jog Dial Control",
                imguiFunc = () => ImGui.Checkbox("##UseJogDialControl", ref UserSettings.Config.UseJogDialControl)
            },

            new UIControlledSetting()
            {
                label = "Scroll smoothing",
                imguiFunc = () => ImGui.DragFloat("##ScrollSmoothing", ref UserSettings.Config.ScrollSmoothing)
            },

            new UIControlledSetting()
            {
                label = "Show Graph thumbnails",
                imguiFunc = () => ImGui.Checkbox("##ShowThumbnails", ref UserSettings.Config.ShowThumbnails)
            },

            new UIControlledSetting()
            {
                label = "Drag snapped nodes",
                imguiFunc = () => ImGui.Checkbox("##SmartGroupDragging", ref UserSettings.Config.SmartGroupDragging)
            },

            new UIControlledSetting()
            {
                label = "Snap strength",
                imguiFunc = () => ImGui.DragFloat("##SnapStrength", ref UserSettings.Config.SnapStrength)
            },

            new UIControlledSetting()
            {
                label = "Click threshold",
                imguiFunc = () => ImGui.DragFloat("##ClickThreshold", ref UserSettings.Config.ClickThreshold)
            },

            new UIControlledSetting()
            {
                label = "Timeline Raster Density",
                imguiFunc = () => ImGui.DragFloat("##TimeRasterDensity", ref UserSettings.Config.TimeRasterDensity, 0.01f)
            },

            new UIControlledSetting()
            {
                label = "Fullscreen Window Swap",
                tooltip = "Swap main and second windows when fullscreen",
                imguiFunc = () => ImGui.Checkbox("##SwapMainAnd2ndWindowsWhenFullscreen", ref UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen)
            },
        };

        static readonly UIControlledSetting[] spaceMouseSettings = new UIControlledSetting[]
        {
            new UIControlledSetting()
            {
                label = "Smoothing",
                imguiFunc = () => ImGui.DragFloat("##SpaceMouseDamping", ref UserSettings.Config.SpaceMouseDamping, 0.01f, 0.01f, 1f)
            },

            new UIControlledSetting()
            {
                label = "Move Speed",
                imguiFunc = () => ImGui.DragFloat("##SpaceMouseMoveSpeedFactor", ref UserSettings.Config.SpaceMouseMoveSpeedFactor, 0.01f, 0, 10f)
            },

            new UIControlledSetting()
            {
                label = "Rotation Speed",
                imguiFunc = () => ImGui.DragFloat("##SpaceMouseRotationSpeedFactor", ref UserSettings.Config.SpaceMouseRotationSpeedFactor, 0.01f, 0, 10f)
            }
        };

        static readonly UIControlledSetting[] additionalSettings = new UIControlledSetting[]
        {
            new UIControlledSetting()
            {
                label = "Gizmo size",
                imguiFunc = () => ImGui.DragFloat("##GizmoSize", ref UserSettings.Config.GizmoSize)
            },

            new UIControlledSetting()
            {
                label = "Tooltip delay",
                imguiFunc = () => ImGui.DragFloat("##TooltipDelay", ref UserSettings.Config.TooltipDelay)
            },

            //new SettingInfo()
            //{
            //    label = "Show Title",
            //    imguiFunc = () => ImGui.Checkbox("##ShowTitleAndDescription", ref UserSettings.Config.ShowTitleAndDescription)
            //},

            //new SettingInfo()
            //{
            //    label = "Show Timeline",
            //    imguiFunc = () => ImGui.Checkbox("##ShowTimeline", ref UserSettings.Config.ShowTimeline)
            //}
        };
        
        static readonly UIControlledSetting[] debugSettings = new UIControlledSetting[]
        {
            new UIControlledSetting()
            {
                label = "VSync",
                imguiFunc = () => ImGui.Checkbox("##UseVSync", ref UseVSync)
            },

            new UIControlledSetting()
            {
                label = "Show Window Regions",
                imguiFunc = () => ImGui.Checkbox("##WindowRegionsVisible", ref WindowRegionsVisible)
            },

            new UIControlledSetting()
            {
                label = "Show Item Regions",
                imguiFunc = () => ImGui.Checkbox("##ItemRegionsVisible", ref ItemRegionsVisible)
            },
        };

        static readonly UIControlledSetting[] t3UiStyleSettings = new UIControlledSetting[]
        {
            new UIControlledSetting()
            {
                label = "Height Connection Zone",
                imguiFunc = () => ImGui.DragFloat("##UsableSlotThickness", ref GraphNode.UsableSlotThickness)
            },

            new UIControlledSetting()
            {
                label = "Label position",
                imguiFunc = () => ImGui.DragFloat2("##LabelPos", ref GraphNode.LabelPos)
            },

            new UIControlledSetting()
            {
                label = "Slot Gaps",
                imguiFunc = () => ImGui.DragFloat("##SlotGaps", ref GraphNode.SlotGaps, 0.1f, 0, 10f)
            },

            new UIControlledSetting()
            {
                label = "Input Slot Margin Y",
                imguiFunc = () => ImGui.DragFloat("##InputSlotMargin", ref GraphNode.InputSlotMargin, 0.1f, 0, 10f)
            },

            new UIControlledSetting()
            {
                label = "Input Slot Thickness",
                imguiFunc = () => ImGui.DragFloat("##InputSlotThickness", ref GraphNode.InputSlotThickness, 0.1f, 0, 10f)
            },

            new UIControlledSetting()
            {
                label = "Output Slot Margin",
                imguiFunc = () => ImGui.DragFloat("##OutputSlotMargin", ref GraphNode.OutputSlotMargin, 0.1f, 0, 10f)
            },

            new UIControlledSetting()
            {
                label = "Value Label Color",
                imguiFunc = () => ImGui.ColorEdit4("##ValueLabelColor", ref T3Style.Colors.ValueLabelColor.Rgba)
            },

            new UIControlledSetting()
            {
                label = "Value Label Color Hover",
                imguiFunc = () => ImGui.ColorEdit4("##ValueLabelColorHover", ref T3Style.Colors.ValueLabelColorHover.Rgba)
            },
        };
    }
}