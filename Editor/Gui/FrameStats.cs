using System;
using System.Collections.Generic;

namespace T3.Editor.Gui
{
    /// <summary>
    /// A helper class that collects information duration the processing of a frame,
    /// so they can be used in the next.   
    /// </summary>
    public static class FrameStats
    {
        public static void AddHoveredId(Guid id)
        {
            Current.HoveredIds.Add(id);
        }

        public static void AddPinnedId(Guid id)
        {
            Current.RenderedIds.Add(id);
        }

        public static void CompleteFrame()
        {
            (Current, Last) = (Last, Current);
            Current.Clear();
        }
            
        public class Stats
        {
            public readonly HashSet<Guid> HoveredIds = new();
            public readonly HashSet<Guid> RenderedIds = new();
            public bool HasKeyframesBeforeCurrentTime;
            public bool HasKeyframesAfterCurrentTime;
            public bool HasAnimatedParameters => HasKeyframesBeforeCurrentTime || HasKeyframesAfterCurrentTime;
            public bool IsItemContextMenuOpen;
            public bool UiColorsChanged;
            public bool SomethingWithTooltipHovered;
            
            /// <summary>
            /// This is reset on Frame start and can be useful for allow context menu to stay open even if a
            /// later context menu would also be opened. There is probably some ImGui magic to do this probably. 
            /// </summary>
            public string OpenedPopUpName;
            
            public void Clear()
            {
                HoveredIds.Clear();
                RenderedIds.Clear();
                HasKeyframesBeforeCurrentTime = false;
                HasKeyframesAfterCurrentTime = false;
                IsItemContextMenuOpen = false;
                UiColorsChanged = false;
                OpenedPopUpName = string.Empty;
                SomethingWithTooltipHovered = false;
            }

        }
        public static Stats Current = new();
        public static Stats Last = new();
    }
}