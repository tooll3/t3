using T3.Core.Operator;

namespace T3.Gui.graph
{
    /// <summary>
    /// 
    /// Converts the relative position of a Connection-DropOver-Event above
    /// an Operator into a meaningful representation that can be used to
    /// highlight the UI and indicate possible drop zones:
    /// 
    /// Is uses the connection and the Operator to compute:
    /// - A list of input zones
    /// - The currently active input zone
    /// - The type and of the currently active input zone
    /// - An indication if the "select parameter from list"-zone is active
    /// 
    /// </summary>
    public class VisibleInputSlot
    {
        public Symbol.InputDefinition InputDefinition;
        public float XInItem;
        public float Width = 1;
        //public ImRect AreaInItem;
        //public bool IsBelowMouse;
        public int MultiInputIndex;
        public bool InsertAtMultiInputIndex;
    }
}