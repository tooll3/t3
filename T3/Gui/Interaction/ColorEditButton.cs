using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using UiHelpers;

namespace T3.Gui.Interaction
{
    public static class ColorEditButton
    {
        public static bool Draw(ref Vector4 color, Vector2 size)
        {
            var buttonPosition = ImGui.GetCursorScreenPos();
            if (ImGui.ColorButton("##thumbnail", color, ImGuiColorEditFlags.AlphaPreviewHalf, size))
            {
                ImGui.OpenPopup("##colorEdit");
            }

            if (ImGui.IsItemActivated())
            {
                _previousColor = color;
                CollectNewColorsInPalette(color);
            }

            var edited = false;
            edited |= HandleQuickSliders(ref color, buttonPosition);
            edited |= DrawPopup(ref color, _previousColor, ImGuiColorEditFlags.AlphaBar);
            return edited;
        }

        private static bool DrawPopup(ref Vector4 color, Vector4 previousColor, ImGuiColorEditFlags flags)
        {
            var edited = false;
            if (ImGui.BeginPopup("##colorEdit"))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                ImGui.Separator();
                edited |= ImGui.ColorPicker4("##picker", ref color, flags | ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview);
                ImGui.SameLine();

                ImGui.BeginGroup(); // Lock X position

                ImGui.ColorButton("##current", color, ImGuiColorEditFlags.NoSmallPreview | ImGuiColorEditFlags.AlphaPreviewHalf,
                                  new Vector2(ImGui.GetContentRegionAvail().X, 40));

                if (ImGui.ColorButton("##previous", previousColor, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreviewHalf,
                                      new Vector2(ImGui.GetContentRegionAvail().X, 40)))
                    color = previousColor;

                ImGui.Separator();

                for (int n = 0; n < ColorPalette.Length; n++)
                {
                    ImGui.PushID(n);
                    if ((n % 8) != 0)
                        ImGui.SameLine(0.0f, 1); //ImGui.GetStyle().ItemSpacing.Y);

                    if (ImGui.ColorButton("##palette", ColorPalette[n],
                                          ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.AlphaPreviewHalf,
                                          new Vector2(20, 20)))

                        color = new Vector4(ColorPalette[n].X, ColorPalette[n].Y, ColorPalette[n].Z, color.W); // Preserve alpha!

                    // Allow user to drop colors into each palette entry
                    // (Note that ColorButton is already a drag source by default, unless using ImGuiColorEditFlags.NoDragDrop)
                    if (ImGui.BeginDragDropTarget())
                    {
                        // TODO: accepting the payload doesn't work because for colorButtons the payload is always undefined.
                        // I'm not sure if this is a problem of ImGui.net. A workaround would be to reimplement ImGui color button. 
                        // var payload = ImGui.AcceptDragDropPayload("_COL4F");
                        // if (ImGui.IsMouseReleased(0))
                        // {
                        //     var color2 = Marshal.PtrToStructure<Vector4>(payload.Data);
                        //     Log.Debug("color:" + color2);
                        // }
                    }

                    ImGui.PopID();
                }

                ImGui.EndGroup();
                ImGui.PopStyleColor();

                ImGui.EndPopup();
            }

            return edited;
        }

        private static void CollectNewColorsInPalette(Vector4 potentialColor)
        {
            var alreadyExists = ColorPalette.Any(c => c == potentialColor);
            if (alreadyExists)
                return;

            ColorPalette[_colorPaletteIndex++ % ColorPalette.Length] = potentialColor;
        }



        private static bool HandleQuickSliders(ref Vector4 color, Vector2 buttonPosition)
        {
            var edited = false;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _rightClickedItemId = ImGui.GetID(string.Empty);
                _previousColor = color;
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            {
                _rightClickedItemId = 0;
            }

            var pCenter = buttonPosition + Vector2.One * ImGui.GetFrameHeight() / 2;

            var showAlphaSlider = ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.IsItemActive();
            if (showAlphaSlider)
            {
                var valuePos = color.W;
                VerticalColorSlider(color, pCenter, valuePos);

                color.W = (_previousColor.W - ImGui.GetMouseDragDelta().Y / 100).Clamp(0, 1);
                edited = true;
            }

            var showBrightnessSlider = ImGui.IsMouseDragging(ImGuiMouseButton.Right) && ImGui.GetID(string.Empty) == _rightClickedItemId;
            if (showBrightnessSlider)
            {
                var hsb = new Color(color).AsHsl;
                var previousHsb = new Color(_previousColor).AsHsl;

                var valuePos = hsb.Z;
                VerticalColorSlider(color, pCenter, valuePos);

                var newBrightness = (previousHsb.Z - ImGui.GetMouseDragDelta(ImGuiMouseButton.Right).Y / 100).Clamp(0, 1);
                color = Color.ColorFromHsl(previousHsb.X, previousHsb.Y, newBrightness, _previousColor.W);
                edited = true;
            }

            return edited;
        }

        public static void VerticalColorSlider(Vector4 color, Vector2 pCenter, float valuePos)
        {
            const int barHeight = 100;
            const int barWidth = 10;
            var drawList = ImGui.GetForegroundDrawList();
            var pMin = pCenter + new Vector2(15, -barHeight * valuePos);
            var pMax = pMin + new Vector2(barWidth, barHeight);
            var area = new ImRect(pMin, pMax);
            drawList.AddRectFilled(pMin - Vector2.One, pMax + Vector2.One, new Color(0.1f, 0.1f, 0.1f));
            CustomComponents.FillWithStripes(drawList, area);

            // Draw Slider
            var opaqueColor = color;
            opaqueColor.W = 1;
            var transparentColor = color;
            transparentColor.W = 0;
            drawList.AddRectFilledMultiColor(pMin, pMax,
                                             ImGui.ColorConvertFloat4ToU32(transparentColor),
                                             ImGui.ColorConvertFloat4ToU32(transparentColor),
                                             ImGui.ColorConvertFloat4ToU32(opaqueColor),
                                             ImGui.ColorConvertFloat4ToU32(opaqueColor));

            drawList.AddRectFilled(pCenter, pCenter + new Vector2(barWidth + 15, 1), Color.Black);
        }

        public static void ColorWheelPicker(Vector4 color, Vector2 pCenter, float valuePos)
        {
            const int barHeight = 100;
            const int barWidth = 10;
            var drawList = ImGui.GetForegroundDrawList();
            var pMin = pCenter + new Vector2(15, -barHeight * valuePos);
            var pMax = pMin + new Vector2(barWidth, barHeight);
            var area = new ImRect(pMin, pMax);
            drawList.AddRectFilled(pMin - Vector2.One, pMax + Vector2.One, new Color(0.1f, 0.1f, 0.1f));
            CustomComponents.FillWithStripes(drawList, area);

            // Draw Slider
            var opaqueColor = color;
            opaqueColor.W = 1;
            var transparentColor = color;
            transparentColor.W = 0;
            drawList.AddRectFilledMultiColor(pMin, pMax,
                                             ImGui.ColorConvertFloat4ToU32(transparentColor),
                                             ImGui.ColorConvertFloat4ToU32(transparentColor),
                                             ImGui.ColorConvertFloat4ToU32(opaqueColor),
                                             ImGui.ColorConvertFloat4ToU32(opaqueColor));

            drawList.AddRectFilled(pCenter, pCenter + new Vector2(barWidth + 15, 1), Color.Black);
        }
        
        
        
        private static Vector4[] IntializePalette(int length)
        {
            var r = new Vector4[length];
            for (int i = 0; i < length; i++)
            {
                r[i] = new Vector4(0, 0, 0, 1);
            }

            return r;
        }

        private static uint _rightClickedItemId;
        private static readonly Vector4[] ColorPalette = IntializePalette(32);
        private static int _colorPaletteIndex;
        private static Vector4 _previousColor;
    }
}