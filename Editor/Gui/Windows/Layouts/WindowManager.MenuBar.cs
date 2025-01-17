using ImGuiNET;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.SystemUi;

namespace T3.Editor.Gui.Windows.Layouts;

internal static partial class WindowManager
{
    public static void DrawWindowMenuContent()
    {
        if (_windows == null)
        {
            Log.Warning("Can't draw window list before initialization");
            return;
        }

        foreach (var window in _windows)
        {
            window.DrawMenuItemToggle();
        }

        ImGui.Separator();

        if (ImGui.MenuItem("2nd Render Window", "", ShowSecondaryRenderWindow))
            ShowSecondaryRenderWindow = !ShowSecondaryRenderWindow;

        var screens = EditorUi.Instance.AllScreens;
        if (ImGui.BeginMenu("2nd Render Window Fullscreen On"))
        {
            for (var index = 0; index < screens.Count; index++)
            {
                var screen = screens.ElementAt(index);
                var label = $"{screen.DeviceName.Trim(new char[] { '\\', '.' })}" +
                            $" ({screen.Bounds.Width}x{screen.Bounds.Height})";
                if (ImGui.MenuItem(label, "", index == UserSettings.Config.FullScreenIndexViewer))
                {
                    UserSettings.Config.FullScreenIndexViewer = index;
                }
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Editor Window Fullscreen On"))
        {
            for (var index = 0; index < screens.Count; index++)
            {
                var screen = screens.ElementAt(index);
                var label = $"{screen.DeviceName.Trim(new char[] { '\\', '.' })}" +
                            $" ({screen.Bounds.Width}x{screen.Bounds.Height})";
                if (ImGui.MenuItem(label, "", index == UserSettings.Config.FullScreenIndexMain))
                {
                    UserSettings.Config.FullScreenIndexMain = index;
                }
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.BeginMenu("Debug"))
        {
            if (ImGui.MenuItem("ImGUI Demo", "", _demoWindowVisible))
                _demoWindowVisible = !_demoWindowVisible;

            if (ImGui.MenuItem("ImGUI Metrics", "", _metricsWindowVisible))
                _metricsWindowVisible = !_metricsWindowVisible;

            ImGui.EndMenu();
        }

        ImGui.Separator();

        LayoutHandling.DrawMainMenuItems();
    }
}