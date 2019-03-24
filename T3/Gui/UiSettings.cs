using ImGuiNET;

namespace T3.Gui
{
    public class UiSettings
    {
        //private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
        public static bool UseVSync => _vsync;
        private static bool _vsync = true;

        public static bool WindowRegionsVisible;
        public static bool ItemRegionsVisible;
        public static bool DemoWindowVisible;

        public static unsafe void DrawUiSettings()
        {
            ImGui.Begin("Stats");
            {
                float framerate = ImGui.GetIO().Framerate;
                ImGui.Text($"Application average {1000.0f / framerate:0.00} ms/frame ({framerate:0.0} FPS)");
                ImGui.Checkbox("VSync", ref _vsync);
                ImGui.Checkbox("Show Window Regions", ref WindowRegionsVisible);
                ImGui.Checkbox("Show Item Regions", ref ItemRegionsVisible);
                ImGui.Checkbox("Demo Window Visible", ref DemoWindowVisible);

                if (ImGui.Button("Open new Graph Canvas"))
                {
                    T3UI.OpenNewGraphWindow();
                }
            }
            ImGui.End();
        }
    }
}
