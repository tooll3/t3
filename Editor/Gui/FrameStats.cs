namespace T3.Editor.Gui;

/// <summary>
/// A helper class that collects information duration the processing of a frame,
/// so they can be used in the next.   
/// </summary>
public static class FrameStats
{
    public static void CompleteFrame()
    {
        (Current, Last) = (Last, Current);
        Current.Clear();
    }
            
    public class Stats
    {
        public bool HasKeyframesBeforeCurrentTime;
        public bool HasKeyframesAfterCurrentTime;
        public bool HasAnimatedParameters => HasKeyframesBeforeCurrentTime || HasKeyframesAfterCurrentTime;
        public bool IsItemContextMenuOpen;
        public bool OpenedPopupCapturedMouse;
        public bool OpenedPopupHovered;
        public bool UiColorsChanged;
        public bool SomethingWithTooltipHovered;
        public bool UndoRedoTriggered;
            
        /// <summary>
        /// This is reset on Frame start and can be useful for allow context menu to stay open even if a
        /// later context menu would also be opened. There is probably some ImGui magic to do this probably. 
        /// </summary>
        public string OpenedPopUpName;
            
        public void Clear()
        {
            HasKeyframesBeforeCurrentTime = false;
            HasKeyframesAfterCurrentTime = false;
            IsItemContextMenuOpen = false;
            UiColorsChanged = false;
            OpenedPopUpName = string.Empty;
            OpenedPopupCapturedMouse = false;
            OpenedPopupHovered = false;
            SomethingWithTooltipHovered = false;
            UndoRedoTriggered = false;
        }

    }
    public static Stats Current = new();
    public static Stats Last = new();
}