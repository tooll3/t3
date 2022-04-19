using ImGuiNET;
using System.Numerics;
using T3.Core;
using UiHelpers;

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
            ImGui.TextUnformatted(((char)(int)icon).ToString());
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
            ImGui.Image(fonts.TexID, area.GetSize(), new Vector2(g.V0, g.U0), new Vector2(g.V1, g.U1));
        }

        public static void DrawIconAtScreenPosition(Icon icon, Vector2 screenPos)
        {
            GetGlyphDefinition(icon, out var uvRange, out var size);
            ImGui.GetWindowDrawList().AddImage(ImGui.GetIO().Fonts.TexID,
                                               screenPos,
                                               screenPos + size,
                                               uvRange.Min,
                                               uvRange.Max,
                                               Color.White);
        }

        public static void DrawIconAtScreenPosition(Icon icon, 
                                                    Vector2 screenPos, 
                                                    ImDrawListPtr drawList)
        {
            GetGlyphDefinition(icon, out var uvRange, out var size);
            drawList.AddImage(ImGui.GetIO().Fonts.TexID,
                              screenPos,
                              screenPos + size,
                              uvRange.Min,
                              uvRange.Max,
                              Color.White);
        }

        public static void DrawIconAtScreenPosition(Icon icon, 
                                                    Vector2 screenPos, 
                                                    ImDrawListPtr drawList,
                                                    Color color)
        {
            GetGlyphDefinition(icon, out var uvRange, out var size);
            drawList.AddImage(ImGui.GetIO().Fonts.TexID,
                              screenPos,
                              screenPos + size,
                              uvRange.Min,
                              uvRange.Max,
                              color);
        }

        public static void DrawIconOnLastItem(Icon icon)
        {
            var pos = ImGui.GetItemRectMin();
            var size = ImGui.GetItemRectMax() - pos;
            GetGlyphDefinition(icon, out var uvRange, out var iconSize);
            var centerOffset = MathUtils.Floor((size - iconSize)/2);
            var alignedPos = pos + centerOffset;
            ImGui.GetWindowDrawList().AddImage(ImGui.GetIO().Fonts.TexID,
                                               alignedPos,
                                               alignedPos + iconSize,
                                               uvRange.Min,
                                               uvRange.Max,
                                               Color.White);
        }
        
        private static void GetGlyphDefinition(Icon icon, out ImRect uvRange, out Vector2 size)
        {
            ImFontGlyphPtr g = IconFont.FindGlyph((char)icon);
            uvRange = GetCorrectUvRangeFromBrokenGlyphStructure(g);
            size = GetCorrectSizeFromBrokenGlyphStructure(g);
        }

        /// <summary>
        /// It looks like ImGui.net v1.83 returns a somewhat strange glyph definition. 
        /// </summary>
        private static ImRect GetCorrectUvRangeFromBrokenGlyphStructure(ImFontGlyphPtr g)
        {
            return new ImRect(             //-- U  -- V ---
                              new Vector2(g.X1,   g.Y1),    // Min    
                              new Vector2(g.U0, g.V0)   // Max
                              );
        }

        /// <summary>
        /// It looks like ImGui.net v1.77 returns a somewhat corrupted glyph. 
        /// </summary>
        private static Vector2 GetCorrectSizeFromBrokenGlyphStructure(ImFontGlyphPtr g)
        {
            return new Vector2(g.X0, g.Y0);
        }

        /// <summary>
        /// Draws a icon in the center of the current imgui item
        /// </summary>
        public static void DrawCentered(Icon icon)
        {
            var g = IconFont.FindGlyph((char)icon);
            var iconSize = new Vector2(g.X1 - g.X0, g.Y1 - g.Y0) / 2;
            var center = (ImGui.GetItemRectMax() + ImGui.GetItemRectMin()) / 2 - iconSize;
            Draw(icon, center);
        }

        public class IconSource
        {
            public IconSource(Icon icon, int slotIndex)
            {
                SourceArea = ImRect.RectWithSize(new Vector2(SlotSize * slotIndex, 0), new Vector2(16,16));
                Char = (char)icon;
            }            
            
            public IconSource(Icon icon, int slotIndex, Vector2 size)
            {
                SourceArea = ImRect.RectWithSize(new Vector2(SlotSize * slotIndex, 0), size);
                Char = (char)icon;
            }
            
            public IconSource(Icon icon, Vector2 pos, Vector2 size)
            {
                SourceArea = ImRect.RectWithSize(pos, size);
                Char = (char)icon;
            }

            private const int SlotSize = 16;
            public readonly ImRect SourceArea;
            public readonly char Char;
        }
        
        

        public static readonly IconSource[] CustomIcons =
            {
                new IconSource(Icon.DopeSheetKeyframeLinearSelected, 0, new Vector2(16, 25)),
                new IconSource(Icon.DopeSheetKeyframeLinear, 1, new Vector2(16, 25)),
                new IconSource(Icon.LastKeyframe, 2, new Vector2(16, 25)),
                new IconSource(Icon.FirstKeyframe, 3, new Vector2(16, 25)),
                new IconSource(Icon.JumpToRangeStart, 4),
                new IconSource(Icon.JumpToPreviousKeyframe, 5),
                new IconSource(Icon.PlayBackwards, 6),
                new IconSource(Icon.PlayForwards, 7),
                new IconSource(Icon.JumpToNextKeyframe, 8),
                new IconSource(Icon.JumpToRangeEnd, 9),
                new IconSource(Icon.Loop, 10, new Vector2(32, 16)),
                new IconSource(Icon.BeatGrid, 12),
                new IconSource(Icon.ConnectedParameter, 13),
                new IconSource(Icon.Stripe4PxPattern, 14),
                new IconSource(Icon.CurveKeyframe, 15),
                new IconSource(Icon.CurveKeyframeSelected, 16),
                new IconSource(Icon.CurrentTimeMarkerHandle, 17),
                new IconSource(Icon.FollowTime, 18),
                new IconSource(Icon.ToggleAudioOn, 19),
                new IconSource(Icon.ToggleAudioOff, 20),
                new IconSource(Icon.Warning, 21),
                new IconSource(Icon.HoverPreviewSmall, 22),
                new IconSource(Icon.HoverPreviewPlay, 23),
                new IconSource(Icon.HoverPreviewDisabled, 24),
                new IconSource(Icon.ConstantKeyframeSelected, 25, new Vector2(16, 25)),
                new IconSource(Icon.ConstantKeyframe, 26, new Vector2(16, 25)),
                new IconSource(Icon.ChevronLeft, 27),
                new IconSource(Icon.ChevronRight, 28),
                new IconSource(Icon.ChevronUp, 29),
                new IconSource(Icon.ChevronDown, 30),
                new IconSource(Icon.Pin, 31),
                new IconSource(Icon.HeartOutlined, 32),
                new IconSource(Icon.Heart, 33),
                new IconSource(Icon.Trash, 34),
                new IconSource(Icon.Grid, 35),
                new IconSource(Icon.Revert, 36),
                
                new IconSource(Icon.DopeSheetKeyframeSmoothSelected, 37, new Vector2(16, 25)),
                new IconSource(Icon.DopeSheetKeyframeSmooth, 38, new Vector2(16, 25)),
                
                new IconSource(Icon.DopeSheetKeyframeCubicSelected, 39, new Vector2(16, 25)),
                new IconSource(Icon.DopeSheetKeyframeCubic, 40, new Vector2(16, 25)),
                new IconSource(Icon.DopeSheetKeyframeHorizontalSelected, 41, new Vector2(16, 25)),
                new IconSource(Icon.DopeSheetKeyframeHorizontal, 42, new Vector2(16, 25)),
                
                new IconSource(Icon.KeyframeToggleOnBoth, new Vector2(43 * 16, 0), new Vector2(23, 15)),
                new IconSource(Icon.KeyframeToggleOnLeft, new Vector2(45 * 16, 0), new Vector2(23, 15)),
                new IconSource(Icon.KeyframeToggleOnRight, new Vector2(47 * 16, 0), new Vector2(23, 15)),
                new IconSource(Icon.KeyframeToggleOnNone, new Vector2(49 * 16, 0), new Vector2(23, 15)),
                
                new IconSource(Icon.KeyframeToggleOffBoth, new Vector2(43 * 16, 16), new Vector2(23, 15)),
                new IconSource(Icon.KeyframeToggleOffLeft, new Vector2(45 * 16, 16), new Vector2(23, 15)),
                new IconSource(Icon.KeyframeToggleOffRight, new Vector2(47 * 16, 16), new Vector2(23, 15)),
                new IconSource(Icon.KeyframeToggleOffNone, new Vector2(49 * 16, 16), new Vector2(23, 15)),
                new IconSource(Icon.Checkmark,  51),
                new IconSource(Icon.Settings,  52),
            };

        public const string IconAtlasPath = @"Resources\t3\t3-icons.png";
    }

    public enum Icon
    {
        DopeSheetKeyframeLinearSelected = 64,
        DopeSheetKeyframeLinear,
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
        ConstantKeyframeSelected,
        ConstantKeyframe,
        ChevronLeft,
        ChevronRight,
        ChevronUp,
        ChevronDown,
        Pin,
        HeartOutlined,
        Heart,
        Trash,
        Grid,
        Revert,
        DopeSheetKeyframeSmoothSelected,
        DopeSheetKeyframeSmooth,
        DopeSheetKeyframeCubicSelected,
        DopeSheetKeyframeCubic,
        DopeSheetKeyframeHorizontalSelected,
        DopeSheetKeyframeHorizontal,
        KeyframeToggleOnBoth,
        KeyframeToggleOnLeft,
        KeyframeToggleOnRight,
        KeyframeToggleOnNone,
        KeyframeToggleOffBoth,
        KeyframeToggleOffLeft,
        KeyframeToggleOffRight,
        KeyframeToggleOffNone,
        Checkmark,
        Settings
    }
}