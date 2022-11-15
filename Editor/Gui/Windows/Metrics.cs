using System;
using ImGuiNET;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Editor.Gui.Styling;
using Editor.Gui.UiHelpers;
using T3.Core;
using T3.Core.IO;

namespace Editor.Gui.Windows
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
            RenderDurationPlot.Draw(_uiRenderDurationMs);
            DeltaTime.Draw(ImGui.GetIO().DeltaTime * 1000);
            ImGui.TextUnformatted("Vertices:" + ImGui.GetIO().MetricsRenderVertices);
            DrawPressedKeys();
        }

        private static readonly Color ColorForUiBar = new Color(0.6f);
        private static readonly Color ColorForFramerateBar = new Color(0.3f);
        private const float ExpectedFramerate = 60;
        private const float ExpectedFrameDurationMs = 1 / ExpectedFramerate * 1000;

        private static float _peakUiRenderDurationMs;
        private static float _peakDeltaTimeMs;

        public static void DrawRenderPerformanceGraph()
        {
            var offsetFromAppMenu = new Vector2(100, 6);
            var screenPosition = ImGui.GetCursorScreenPos() + offsetFromAppMenu;
            
            const float barWidth = 120;
            const float barHeight = 3;
            ImGui.SameLine(0, offsetFromAppMenu.X);
            if (ImGui.InvisibleButton("performanceGraph", new Vector2(barWidth, ImGui.GetFrameHeight())))
            {
                T3Ui.UseVSync = !T3Ui.UseVSync;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"UI: {_peakUiRenderDurationMs:0.0}ms\nRender: {_peakDeltaTimeMs:0.0}ms\n VSync: {(T3Ui.UseVSync?"On":"Off")} (Click to toggle)");
            }
            const float normalFramerateLevelAt = 0.5f;
            const float frameTimingScaleFactor = barWidth / normalFramerateLevelAt / ExpectedFramerate;

            _peakUiRenderDurationMs = _peakUiRenderDurationMs > _uiRenderDurationMs
                                          ? MathUtils.Lerp(_peakUiRenderDurationMs, _uiRenderDurationMs, 0.05f)
                                          : _uiRenderDurationMs;
            


            var deltaTimeMs = ImGui.GetIO().DeltaTime * 1000;
            if (deltaTimeMs > ExpectedFrameDurationMs * 0.8f && deltaTimeMs < ExpectedFrameDurationMs * 1.25f)
            {
                deltaTimeMs = ExpectedFrameDurationMs;
            }
            
            _peakDeltaTimeMs = _peakDeltaTimeMs > deltaTimeMs
                                   ? MathUtils.Lerp(_peakDeltaTimeMs, deltaTimeMs, 0.05f)
                                   : deltaTimeMs;
            
            var drawList = ImGui.GetWindowDrawList();

            // Draw Ui Render Duration
            var uiTimeWidth = (float)Math.Ceiling(_uiRenderDurationMs * frameTimingScaleFactor);
            drawList.AddRectFilled(screenPosition, screenPosition + new Vector2(uiTimeWidth, barHeight), ColorForUiBar);

            // Draw Frame Render Duration
            var deltaTimeWidth = deltaTimeMs * frameTimingScaleFactor - uiTimeWidth;
            var renderBarPos = screenPosition + new Vector2(uiTimeWidth, 0);
            drawList.AddRectFilled(renderBarPos, renderBarPos + new Vector2(deltaTimeWidth, barHeight), ColorForFramerateBar);

            // Draw Peak UI Duration
            var peakUiTimePos = screenPosition + new Vector2((int)(_peakUiRenderDurationMs * frameTimingScaleFactor), 0);
            drawList.AddRectFilled(peakUiTimePos, peakUiTimePos + new Vector2(2, barHeight), ColorForUiBar);


            // Draw Peak Render Duration
            var peakDeltaTimePos = screenPosition + new Vector2((int)(_peakDeltaTimeMs * frameTimingScaleFactor), 0);
            drawList.AddRectFilled(peakDeltaTimePos, peakDeltaTimePos + new Vector2(2, barHeight), ColorForFramerateBar);
            
            // Draw 60fps mark
            var normalFramerateMarkerPos = screenPosition + new Vector2(ExpectedFrameDurationMs * frameTimingScaleFactor, 0);
            drawList.AddRectFilled(normalFramerateMarkerPos, normalFramerateMarkerPos + new Vector2(1, barHeight + 3), ColorForFramerateBar);
            
            ImGui.PushFont(Fonts.FontSmall);
            drawList.AddText(screenPosition + new Vector2(0, 4), ColorForFramerateBar, $"{deltaTimeMs:0.0}ms");
            ImGui.PopFont();
        }

        /// <summary>
        /// This can be helpful to build keyboard shorts and verify the keys mapping
        /// </summary>
        private static void DrawPressedKeys()
        {
            var io = ImGui.GetIO();
            ImGui.TextUnformatted(
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

            ImGui.TextUnformatted("Pressed keys:" + sb);
        }

        private static float _uiRenderDurationMs;
        private static readonly CurvePlot RenderDurationPlot = new CurvePlot("ms UI") { MinValue = 0, MaxValue = 30f, Damping = true };
        private static readonly CurvePlot DeltaTime = new CurvePlot("ms Frame") { MinValue = 0, MaxValue = 30f };
        private static readonly Stopwatch WatchImgRenderTime = new Stopwatch();
    }
}