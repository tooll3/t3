using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using lib.io.midi;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi
{
    public static class MidiInputUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect, Vector2 canvasScale)
        {
            if (!(instance is MidiInput midiInput)
                || !ImGui.IsRectVisible(screenRect.Min, screenRect.Max))
                return SymbolChildUi.CustomUiResult.None;
            
            
            
            
            // Draw label and current value
            ImGui.SetCursorScreenPos(screenRect.Min);
            ImGui.BeginGroup();
            ImGui.PushClipRect(screenRect.Min, screenRect.Max, true);

            const float flashDuration = 0.6f;
            // Flash on changes
            var flashProgress = (float)(Playback.RunTimeInSecs - midiInput.LastMessageTime).Clamp(0,flashDuration)/flashDuration;
            if (flashProgress < 1)
            {
                drawList.AddRectFilled(screenRect.Min, screenRect.Max, 
                                       Color.Mix(UiColors.StatusAnimated.Fade(0.8f), 
                                                 Color.Transparent, 
                                                 MathF.Pow(flashProgress*flashProgress, 0.5f)));
            }
            

            ImGui.PushFont(Fonts.FontSmall);

            var deviceAndChannel = "Midi Device?";
            if (!string.IsNullOrEmpty(midiInput.Device.TypedInputValue.Value))
            {
                var _displayControlValue = midiInput.Control.TypedInputValue.Value.ToString();
                var _displayChannelValue = midiInput.Channel.TypedInputValue.Value.ToString();
                var _displayDeviceValue = midiInput.Device.TypedInputValue.Value;
                if (midiInput.Control.IsConnected)
                    _displayControlValue = midiInput.Control.DirtyFlag.IsDirty ? "??" : midiInput.Control.Value.ToString();
                if (midiInput.Channel.IsConnected)
                    _displayChannelValue = midiInput.Channel.DirtyFlag.IsDirty ? "??" : midiInput.Channel.Value.ToString();
                if (midiInput.Device.IsConnected)
                    _displayDeviceValue = midiInput.Device.DirtyFlag.IsDirty ? "??" : midiInput.Device.Value;
                deviceAndChannel = $"{_displayDeviceValue} CH{_displayChannelValue}:{_displayControlValue}";
            }

            ImGui.TextUnformatted(deviceAndChannel);

            var renamedTitle = midiInput.Parent.Symbol.Children.Single(c => c.Id == midiInput.SymbolChildId).Name;
            if (!string.IsNullOrEmpty(renamedTitle))
            {
                ImGui.TextUnformatted($"\"{renamedTitle}\"");
            }

            var normalizedFadeOut = ((Playback.RunTimeInSecs - midiInput.LastMessageTime) / 5).Clamp(0, 1);
            var fadeOut = (float)MathUtils.RemapAndClamp(normalizedFadeOut, 0, 1, 1, 0.5f);
            var fadeColor = UiColors.ForegroundFull.Fade(fadeOut);
            ImGui.TextColored(fadeColor, $"{midiInput.Result.Value:0.00}");

            ImGui.PopClipRect();
            ImGui.EndGroup();

            // Drag mini graph
            var graphRect = screenRect;
            const int padding = -3;

            graphRect.Expand(padding);
            if (graphRect.GetHeight() > 0 && graphRect.GetWidth() > 0)
            {
                var minRange = midiInput.OutputRange.TypedInputValue.Value.X;
                var maxRange = midiInput.OutputRange.TypedInputValue.Value.Y;
                var currentValue = midiInput.Result.Value;

                var xPos = MathUtils.RemapAndClamp((double)currentValue, minRange, maxRange, graphRect.Min.X, graphRect.Max.X);
                var topLeftPos = new Vector2((float)xPos, graphRect.Min.Y);
                drawList.AddRectFilled(topLeftPos, topLeftPos + new Vector2(1, graphRect.GetHeight()), UiColors.StatusAnimated);
            }

            ImGui.PopFont();

            return SymbolChildUi.CustomUiResult.Rendered 
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph 
                   | SymbolChildUi.CustomUiResult.PreventInputLabels
                   | SymbolChildUi.CustomUiResult.PreventTooltip;
        }
    }
}
