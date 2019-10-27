using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/**
 * Snap behaviour is implemented through an instance of a ValueSnapHandler and 
 * any number of ValueSnapProviders registring to the SnapHandler. When manipulating 
 * the value by dragging or other interactions, you can constantly check the SnapHandler
 * if the current value would snap to a new value coming from any of the registered
 * SnapProviders.
 */

namespace T3.Gui.Animation.Snapping
{
        ///<summary>Helper struct to pass return values.</summary>
    public class SnapResult
    {
        public SnapResult() { }
        
        public SnapResult(double target, double force)
        {
            SnapToValue = target;
            Force = force;
        }

        public double SnapToValue { get; set; }
        public double Force { get; set; }
    }

    /// <summary>
    /// This is called by the SnapHandler the attractor registered to. should return null if not snapping
    /// </summary>
    public interface IValueSnapAttractor
    {
        SnapResult CheckForSnap(double value);
    }
}
