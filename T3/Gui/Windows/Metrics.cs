using ImGuiNET;
using System.Diagnostics;
using System.Text;
using T3.Gui.UiHelpers;

namespace T3.Gui.Windows
{
    public static class T3Metrics
    {
        public static void UiRenderingStarted()
        {
            WatchImgRenderTime.Restart();
            WatchImgRenderTime.Start();
        }

        public static void UiRenderingCompleted()
        {
            WatchImgRenderTime.Stop();
            _uiRenderDurationMs = (float)((double)WatchImgRenderTime.ElapsedTicks / Stopwatch.Frequency * 1000.0);
        }

        public static void Draw()
        {
            //RenderDurationPlot.Draw(_uiRenderDurationMs);
            RenderDurationPlot.Draw(_uiRenderDurationMs);
            DeltaTime.Draw(ImGui.GetIO().DeltaTime * 1000);
            ImGui.Text("Vertices:"+ImGui.GetIO().MetricsRenderVertices);
            DrawPressedKeys();
        }

        /// <summary>
        /// This can be helpful to build keyboard shorts and verify the keys mapping
        /// </summary>
        private static void DrawPressedKeys()
        {
            var io = ImGui.GetIO();
            ImGui.Text(
                       (io.KeyAlt ? "Alt" : "")
                       + (io.KeyCtrl ? "Ctrl" : "")
                       + (io.KeyShift ? "Shift" : ""));

            var sb = new StringBuilder();
            for (var i = 0; i < ImGui.GetIO().KeysDown.Count; i++)
            {
                if (!io.KeysDown[i])
                    continue;
                
                var k = (Key)i;
                sb.Append($"{k} [{i}]");
            }

            ImGui.Text("Pressed keys:" + sb);
        }

        private static float _uiRenderDurationMs;
        private static readonly CurvePlot RenderDurationPlot = new CurvePlot("ms UI") {MinValue =0, MaxValue = 30f, Damping = true};
        private static readonly CurvePlot DeltaTime = new CurvePlot("ms Frame") {MinValue =0, MaxValue = 30f};
        private static readonly Stopwatch WatchImgRenderTime = new Stopwatch();
    }
}