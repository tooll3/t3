using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;

namespace T3.Editor.Gui.Windows
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
                ImGui.BeginTooltip();
                {
                    ImGui.Text($"UI: {_peakUiRenderDurationMs:0.0}ms\nRender: {_peakDeltaTimeMs:0.0}ms\n VSync: {(T3Ui.UseVSync?"On":"Off")} (Click to toggle)");
                    
                    ImGui.Spacing();
                    
                    ImGui.PushFont(Fonts.FontSmall);

                    foreach (var (key, number) in RenderStatsCollector.ResultsForLastFrame)
                    {
                        var formattedNumber = number switch
                                    {
                                        > 1000000 => $"{number / 1000000.0:0.0}M",
                                        > 1000    => $"{number / 1000.0:0.0}K",
                                        _         => number.ToString()
                                    };

                        ImGui.Text($"{formattedNumber} {key}");
                    }
                    ImGui.PopFont();
                }
                ImGui.EndTooltip();
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
        

        private static uint ColorForUiBar => UiColors.ForegroundFull.Fade(0.6f);
        private static uint ColorForFramerateBar => UiColors.ForegroundFull.Fade(0.3f);
        private const float ExpectedFramerate = 60;
        private const float ExpectedFrameDurationMs = 1 / ExpectedFramerate * 1000;

        private static float _peakUiRenderDurationMs;
        private static float _peakDeltaTimeMs;


        private static float _uiRenderDurationMs;
        private static readonly Stopwatch _watchImgRenderTime = new();
    }
}