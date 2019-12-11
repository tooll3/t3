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
            var keepPosition = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(screenPosition);
            Draw(icon);
            ImGui.SetCursorScreenPos(keepPosition);
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

        /// <summary>
        /// Draws a icon in the center of the current imgui item
        /// </summary>
        public static void DrawCentered(Icon icon)
        {
            var g = IconFont.FindGlyph((char)icon);
            var iconSize = new Vector2(g.X1-g.X0, g.Y1- g.Y0)/2;
            var center = (ImGui.GetItemRectMax() + ImGui.GetItemRectMin()) / 2 - iconSize;
            Draw(icon, center );
        }


        public class IconSource
        {
            public IconSource(Icon icon, int slotIndex, Vector2 size)
            {
                SourceArea = ImRect.RectWithSize(new Vector2(SlotSize * slotIndex, 0), size);
                Char = (char)icon;
            }

            private const int SlotSize = 16;
            public readonly ImRect SourceArea;
            public readonly char Char;
        }

        public static readonly IconSource[] CustomIcons = {
            new IconSource(Icon.KeyFrameSelected, 0, new Vector2(16, 25)),
            new IconSource(Icon.KeyFrame, 1, new Vector2(16, 25)),
            new IconSource(Icon.LastKeyframe, 2, new Vector2(16,25)),
            new IconSource(Icon.FirstKeyframe,3 , new Vector2(16,25)),
            new IconSource(Icon.JumpToRangeStart,4 , new Vector2(16,16)),
            new IconSource(Icon.JumpToPreviousKeyframe,5 , new Vector2(16,16)),
            new IconSource(Icon.PlayBackwards,6 , new Vector2(16,16)),
            new IconSource(Icon.PlayForwards,7 , new Vector2(16,16)),
            new IconSource(Icon.JumpToNextKeyframe,8 , new Vector2(16,16)),
            new IconSource(Icon.JumpToRangeEnd,9 , new Vector2(16,16)),
            new IconSource(Icon.Loop,10 , new Vector2(32,16)),
            new IconSource(Icon.BeatGrid,12 , new Vector2(16,16)),
            new IconSource(Icon.ConnectedParameter,13 , new Vector2(16,16)),
            new IconSource(Icon.Stripe4PxPattern,14, new Vector2(16,16)),
            new IconSource(Icon.CurveKeyframe,15, new Vector2(16,16)),
            new IconSource(Icon.CurveKeyframeSelected,16, new Vector2(16,16)),
            new IconSource(Icon.CurrentTimeMarkerHandle,17, new Vector2(16,16)),
            new IconSource(Icon.FollowTime,18, new Vector2(16,16)),
            new IconSource(Icon.ToggleAudioOn,19, new Vector2(16,16)),
            new IconSource(Icon.ToggleAudioOff,20, new Vector2(16,16)),
            new IconSource(Icon.Warning, 21, new Vector2(16,16)),
            new IconSource(Icon.HoverPreviewSmall, 22, new Vector2(16,16)),
            new IconSource(Icon.HoverPreviewPlay, 23, new Vector2(16,16)),
            new IconSource(Icon.HoverPreviewDisabled, 24, new Vector2(16,16)),
        };

        public const string IconAtlasPath = @"Resources\t3\t3-icons.png";
    }


    public enum Icon
    {
        KeyFrameSelected=64,
        KeyFrame,
        LastKeyframe,
        FirstKeyframe,
        JumpToRangeStart,
        JumpToPreviousKeyframe,
        PlayBackwards,
        PlayForwards,
        JumpToNextKeyframe,
        JumpToRangeEnd,
        Loop,
        BeatGrid,
        ConnectedParameter,
        Stripe4PxPattern,    
        CurveKeyframe,
        CurveKeyframeSelected,
        CurrentTimeMarkerHandle,
        FollowTime,
        ToggleAudioOn,
        ToggleAudioOff,
        Warning,    
        HoverPreviewSmall,
        HoverPreviewPlay,
        HoverPreviewDisabled,
    }
}
