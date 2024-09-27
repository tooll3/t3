namespace T3.Editor.Gui.Selection;

public static class FitViewToSelectionHandling
{
    /// <summary>
    /// This is called at the beginning of each frame.
    /// 
    /// For some events we have to use a frame delay mechanism so ui-elements can
    /// respond to updates in a controlled manner (I.e. when rendering the next frame) 
    /// </summary>
    public static void ProcessNewFrame()
    {
        FitViewToSelectionRequested = _fitViewToSelectionTriggered;
        _fitViewToSelectionTriggered = false;
    }
        
    public static void FitViewToSelection()
    {
        _fitViewToSelectionTriggered = true;
    }

    public static bool FitViewToSelectionRequested { get; private set; }
    private static bool _fitViewToSelectionTriggered = false;        
}