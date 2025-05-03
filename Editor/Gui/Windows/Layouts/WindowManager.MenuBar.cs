#nullable enable
using ImGuiNET;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;

namespace T3.Editor.Gui.Windows.Layouts;

internal static partial class WindowManager
{
    public static void DrawWindowMenuContent()
    {
        foreach (var window in _windows)
        {
            // Settings window is show in help menu...
            if (window == SettingsWindow)
                continue;

            window.DrawMenuItemToggle();
        }

        ImGui.Separator();
        {
            var screens = EditorUi.Instance.AllScreens;

            if (ImGui.MenuItem("Output Window", "", ShowSecondaryRenderWindow))
                ShowSecondaryRenderWindow = !ShowSecondaryRenderWindow;

            if (ImGui.BeginMenu("Output Window Display"))
            {
                for (var index = 0; index < screens.Count; index++)
                {
                    var screen = screens.ElementAt(index);
                    var label = $"{screen.DeviceName.Trim('\\', '.')} ({screen.Bounds.Width}x{screen.Bounds.Height})";
                    if (ImGui.MenuItem(label, "", index == UserSettings.Config.FullScreenIndexViewer))
                    {
                        UserSettings.Config.FullScreenIndexViewer = index;
                    }
                }

                ImGui.EndMenu();
            }
        }

        ImGui.Separator();

        LayoutHandling.DrawMainMenuItems();
    }
}