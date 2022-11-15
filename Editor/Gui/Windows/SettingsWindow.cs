using System.Collections.Generic;
using System.Numerics;
using Editor.Gui.UiHelpers;
using ImGuiNET;

namespace Editor.Gui.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            Config.Title = "Settings";
        }

        protected override void DrawContent()
        {
            var changed = false;
            ImGui.NewLine();
            if (ImGui.TreeNode("User Interface"))
            {
                changed |= CustomComponents.DrawCheckboxParameter("Warn before Lib modifications",
                                                       ref UserSettings.Config.WarnBeforeLibEdit,
                                                       "This warning pops up when you attempt to enter an Operator that ships with the application.\n" +
                                                       "If unsure, this is best left checked.");
                
                changed |= CustomComponents.DrawCheckboxParameter("Use arc connections",
                                                                  ref UserSettings.Config.UseArcConnections,
                                                                  "Affects the shape of the connections between your Operators");
                

                changed |= CustomComponents.DrawCheckboxParameter("Use Jog Dial Control",
                                                                  ref UserSettings.Config.UseJogDialControl,
                                                                  "Affects the shape of the connections between your Operators");

                changed |= CustomComponents.DrawCheckboxParameter("Show Graph thumbnails",
                                                                  ref UserSettings.Config.ShowThumbnails);

                changed |= CustomComponents.DrawCheckboxParameter("Drag snapped nodes",
                                                                  ref UserSettings.Config.SmartGroupDragging);
                
                changed |= CustomComponents.DrawCheckboxParameter("Fullscreen Window Swap",
                                                                  ref UserSettings.Config.SwapMainAnd2ndWindowsWhenFullscreen,
                                                                  "Swap main and second windows when fullscreen");

                ImGui.Dummy(new Vector2(20,20));
                

                changed |= CustomComponents.DrawFloatParameter("UI Scale",
                                                               ref UserSettings.Config.UiScaleFactor,
                                                               0.1f, 5f, 0.01f, true, 
                                                               "The global scale of all rendered UI in the application");
                

                changed |= CustomComponents.DrawFloatParameter("Scroll smoothing",
                                                               ref UserSettings.Config.ScrollSmoothing,
                                                               0.0f, 0.2f, 0.01f, true);

                changed |= CustomComponents.DrawFloatParameter("Snap strength",
                                                    ref UserSettings.Config.SnapStrength,
                                                    0.0f, 0.2f, 0.01f, true);
                
                changed |= CustomComponents.DrawFloatParameter("Click threshold",
                                                               ref UserSettings.Config.ClickThreshold,
                                                               0.0f, 10f, 0.1f, true, 
                                                               "The threshold in pixels until a click becomes a drag. Adjusting this might be useful for stylus input.");
                
                changed |= CustomComponents.DrawFloatParameter("Timeline Raster Density",
                                                               ref UserSettings.Config.TimeRasterDensity,
                                                               0.0f, 10f, 0.01f, true, 
                                                               "The threshold in pixels until a click becomes a drag. Adjusting this might be useful for stylus input.");
                ImGui.Dummy(new Vector2(20,20));
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Space Mouse"))
            {
                CustomComponents.HelpText("These settings only apply with a connected space mouse controller");
                
                changed |= CustomComponents.DrawFloatParameter("Smoothing",
                                                               ref UserSettings.Config.SpaceMouseDamping,
                                                               0.0f, 10f, 0.01f, true);                

                changed |= CustomComponents.DrawFloatParameter("Move Speed",
                                                               ref UserSettings.Config.SpaceMouseMoveSpeedFactor,
                                                               0.0f, 10f, 0.01f, true);                

                changed |= CustomComponents.DrawFloatParameter("Rotation Speed",
                                                               ref UserSettings.Config.SpaceMouseRotationSpeedFactor,
                                                               0.0f, 10f, 0.01f, true);                
                
                ImGui.Dummy(new Vector2(20,20));
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Additional Settings"))
            {
                changed |= CustomComponents.DrawFloatParameter("Gizmo size",
                                                               ref UserSettings.Config.GizmoSize,
                                                               0.0f, 10f, 0.01f, true);                        

                changed |= CustomComponents.DrawFloatParameter("Tooltip delay in Seconds",
                                                               ref UserSettings.Config.TooltipDelay,
                                                               0.0f, 30f, 0.01f, true);        
                
                changed |= CustomComponents.DrawCheckboxParameter("Always show description in Symbol Browser",
                                                               ref UserSettings.Config.AlwaysShowDescriptionPanel,
                                                               "Shifts the Description panel to the left of the Symbol Browser when\n" +
                                                               "it is too close to the right edge of the screen to display it.");
                ImGui.TreePop();
            }
            if (changed)
                UserSettings.Save();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}