namespace T3.Gui.Interaction.Snapping
{
    /// <summary>
    /// Helper struct to pass return values
    /// </summary>
    /// <remarks>
    /// Snap behaviour is implemented through an instance of a ValueSnapHandler and 
    /// any number of ValueSnapProviders regestring to the SnapHandler. When manipulating 
    /// the value by dragging or other interactions, you can constantly check the SnapHandler
    /// if the current value would snap to a new value coming from any of the registered
    /// SnapProviders.
    /// </remarks>
    public abstract class SnapResult
    {
        public double SnapToValue { get; set; }
        public double Force {get; set;}
    }

    
    /// <summary>
    /// This is called by the SnapHandler the attractor registered to.
    /// </summary>
    /// <remarks>should return null if not snapping</remarks>
    public interface IValueSnapAttractor
    {
        SnapResult CheckForSnap(double value);
    }
}