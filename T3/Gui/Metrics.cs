using ImGuiNET;
using System.Diagnostics;
using System.Text;

namespace T3.Gui
{
    public static class T3Metrics
    {
        public static void UiRenderingStarted()
        {
            _watchImgRenderTime.Restart();
            _watchImgRenderTime.Start();
        }

        public static void UiRenderingCompleted()
        {
            _watchImgRenderTime.Stop();
            _uiRenderDurationMs = (float)((double)_watchImgRenderTime.ElapsedTicks / Stopwatch.Frequency * 1000.0);
        }

        public static void Draw()
        {
            DrawRenderDuration();
            DrawVertexCount();
            DrawPressedKeys();

            //float framerate = ImGui.GetIO().Framerate;
            //ImGui.Text($"average {1000.0f / framerate:0.00}ms ({framerate:0.0}FPS) ");
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
                if (io.KeysDown[i])
                {
                    Key k = (Key)i;

                    sb.Append($"{k} [{i}]");
                }
            }
            ImGui.Text("Pressed keys:" + sb);
        }

        private static void DrawRenderDuration()
        {
            _dampedUiRenderDurationMs = _dampedUiRenderDurationMs * (1 - 0.01f) + _uiRenderDurationMs * 0.01f;
            ImGui.PlotLines($"{_dampedUiRenderDurationMs:0.0}ms", ref _renderDurations[0], FramerateSampleCount, _sampleOffset, "", scale_min: 0, scale_max: 10f);
            _renderDurations[_sampleOffset] = _uiRenderDurationMs;
            _sampleOffset = (_sampleOffset + 1) % FramerateSampleCount;
        }

        private static void DrawVertexCount()
        {
            var vertexCount = ImGui.GetIO().MetricsRenderVertices;
            _vertexCounts[_sampleOffset] = vertexCount;
            ImGui.PlotLines($"{vertexCount}verts", ref _vertexCounts[0], FramerateSampleCount, _sampleOffset, "", scale_min: 0, scale_max: 50000);

        }

        private const int FramerateSampleCount = 500;
        private static int _sampleOffset = 0;

        private static Stopwatch _watchImgRenderTime = new Stopwatch();
        private static float _uiRenderDurationMs = 0;
        private static float _dampedUiRenderDurationMs;
        private static float[] _renderDurations = new float[FramerateSampleCount];
        public static float[] _vertexCounts = new float[FramerateSampleCount];
    }
}
