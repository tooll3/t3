using ImGuiNET;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UiHelpers;
using static T3.ImGuiDx11Impl;

namespace T3.Gui.Styling
{
    /// <summary>
    /// Handles the mapping of custom icons
    /// </summary>
    static class Icons
    {
        public static ImFontPtr IconFont { get; set; }

        public static void Draw(Icon icon)
        {
            ImGui.PushFont(IconFont);
            ImGui.Text(((char)(int)icon).ToString());
            ImGui.PopFont();
        }


        public static void Draw(Icon icon, Vector2 screenPosition)
        {
            ImGui.SetCursorScreenPos(screenPosition);
            Draw(icon);
        }


        public static void Draw(Icon icon, Vector2 screenPosition, Color color)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, color.Rgba);
            Draw(icon, screenPosition);
            ImGui.PopStyleColor();
        }


        public static void Draw(Icon icon, ImRect area)
        {
            var fonts = ImGui.GetIO().Fonts;
            var g = IconFont.FindGlyph((char)icon);
            ImGui.SetCursorScreenPos(area.Min);
            ImGui.Image(fonts.TexID, area.GetSize(), new Vector2(g.U0, g.V0), new Vector2(g.U1, g.V1));
        }


        public class IconSource
        {
            public IconSource(Icon icon, int slotIndex, Vector2 size)
            {
                SourceArea = ImRect.RectWithSize(new Vector2(SLOT_SIZE * slotIndex, SLOT_SIZE), size);
                Char = (char)icon;
            }

            private const int SLOT_SIZE = 32;
            public readonly ImRect SourceArea;
            public readonly char Char;
        }


        public static readonly IconSource[] CustomIcons = new IconSource[]
                                                          {
                                                              new IconSource(Icon.KeyFrame, 0, new Vector2(32, 32)),
                                                              new IconSource(Icon.KeyFrameSelected, 4, new Vector2(32, 32)),
                                                              new IconSource(Icon.NextKeyframeDisabled, 6, new Vector2(32, 32)),
                                                              new IconSource(Icon.NextKeyframeEnabled, 5, new Vector2(32, 32)),
                                                          };

        public const string IconAtlasPath = @"Resources\icons.png";
    }


    public enum Icon
    {
        KeyFrame = 64,
        KeyFrameSelected,
        NextKeyframeDisabled,
        NextKeyframeEnabled,
    }
}
