using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Graph.Interaction;
using T3.Gui.InputUi;
using T3.Gui.Selection;

namespace T3.Gui.Interaction
{
    public class ColorEditButton
    {
        public static bool Draw(ref Vector4 color, Vector2 size)
        {
            if (ImGui.ColorButton("##thumbnail", color, ImGuiColorEditFlags.AlphaPreviewHalf, size))
            {
                ImGui.OpenPopup("##colorEdit");
            }

            if (ImGui.IsItemActivated())
            {
                previousColor = color;
                CollectNewColorsInPalette(color);
            }

            if (ImGui.IsItemActivated() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                Log.Debug("Dragging" + ImGui.GetMouseDragDelta());
            }

            var edited = DrawPopup(ref color, previousColor, ImGuiColorEditFlags.AlphaBar);
            // if (edited && !ImGui.IsPopupOpen("##colorEdit"))
            // {
            //     colorPalette[colorPaletteIndex++ % colorPalette.Length] = color;
            // }
            // if (ImGui.BeginPopup("##colorEdit"))
            // {
            //     if (ImGui.ColorPicker4("edit", ref float4Value,
            //                            ImGuiColorEditFlags.Float | ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.HDR))
            //     {
            //         edited = true;
            //     }
            //
            //     ImGui.EndPopup();
            // }

            return edited;
        }

        private static bool DrawPopup(ref Vector4 color, Vector4 previousColor, ImGuiColorEditFlags misc_flags)
        {
            var edited = false;
            if (ImGui.BeginPopup("##colorEdit"))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Gray.Rgba);
                ImGui.Separator();
                edited |= ImGui.ColorPicker4("##picker", ref color, misc_flags | ImGuiColorEditFlags.NoSidePreview | ImGuiColorEditFlags.NoSmallPreview);
                ImGui.SameLine();

                ImGui.BeginGroup(); // Lock X position

                ImGui.ColorButton("##current", color, ImGuiColorEditFlags.NoSmallPreview | ImGuiColorEditFlags.AlphaPreviewHalf,
                                  new Vector2(ImGui.GetContentRegionAvail().X, 40));

                if (ImGui.ColorButton("##previous", previousColor, ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreviewHalf,
                                      new Vector2(ImGui.GetContentRegionAvail().X, 40)))
                    color = previousColor;

                ImGui.Separator();

                for (int n = 0; n < colorPalette.Length; n++)
                {
                    ImGui.PushID(n);
                    if ((n % 8) != 0)
                        ImGui.SameLine(0.0f, 1); //ImGui.GetStyle().ItemSpacing.Y);

                    if (ImGui.ColorButton("##palette", colorPalette[n],
                                           ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.AlphaPreviewHalf, new Vector2(20, 20)))

                        color = new Vector4(colorPalette[n].X, colorPalette[n].Y, colorPalette[n].Z, color.W); // Preserve alpha!

                    // Allow user to drop colors into each palette entry
                    // (Note that ColorButton is already a drag source by default, unless using ImGuiColorEditFlags.NoDragDrop)
                    if (ImGui.BeginDragDropTarget())
                    {
                        var payload = ImGui.AcceptDragDropPayload("_COL4F");
                        
                        // TODO: accepting the payload doesn't work because for colorButtons the payload is always undefined.
                        // I'm not sure if this is a problem of ImGui.net. A workaround would be to reimplement ImGui color button. 
                        
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
            var alreadyExists = colorPalette.Any(c => c == potentialColor);
            if (alreadyExists)
                return;
            
            colorPalette[colorPaletteIndex++ % colorPalette.Length] = potentialColor;
        }
        
        private static Vector4[] IntializePalette(int length)
        { 
            var r = new Vector4[length];
            for (int i = 0; i < length; i++)
            {
                r[i] = new Vector4(0,0,0,1);
            }
            return r;
        }

        private static Vector4[] colorPalette = IntializePalette(32);
        private static int colorPaletteIndex = 0;
        private static Vector4 previousColor;
    }
}