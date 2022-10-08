using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.Windows;

namespace t3.Gui
{
    public static class SettingsUi
    {
        /// <summary>
        /// Draws a table of <see cref="UIControlledSetting"/>s
        /// </summary>
        /// <param name="tableID">Unique identifier for your table - will not be displayed</param>
        /// <param name="settings">Settings to display</param>
        /// <returns>Returns true if a setting has been modified</returns>
        public static bool DrawSettingsTable(string tableID, UIControlledSetting[] settings)
        {
            ImGui.NewLine();
            bool changed = false;
            if (ImGui.BeginTable(tableID, 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.PadOuterX))
            {
                foreach (UIControlledSetting setting in settings)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Indent();

                    if (setting.DrawOnLeft)
                    {
                        var valueChanged = setting.DrawGUIControl(true);
                        changed |= valueChanged;
                        ImGui.SameLine();
                        ImGui.Dummy(_leftCheckboxSpacing);
                        ImGui.SameLine();
                        ImGui.Text(setting.CleanLabel);
                        ImGui.Unindent();
                    }
                    else
                    {
                        ImGui.Text(setting.CleanLabel);
                        ImGui.Unindent();
                        ImGui.TableNextColumn();
                        var valueChanged = setting.DrawGUIControl(true);
                        changed |= valueChanged;
                    }

                }
            }

            ImGui.EndTable();
            ImGui.NewLine();

            return changed;
        }

        /// <summary>
        /// Draws a series of settings in order
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool DrawSettings(UIControlledSetting[] settings)
        {
            ImGui.NewLine();

            bool changed = false;

            foreach (var setting in settings)
            {
                ImGui.Text(setting.CleanLabel);
                ImGui.SameLine();
                changed |= setting.DrawGUIControl(true);
            }

            ImGui.NewLine();
            return changed;
        }

        private static readonly Vector2 _leftCheckboxSpacing = new Vector2(0f, 20f);
    }
}
