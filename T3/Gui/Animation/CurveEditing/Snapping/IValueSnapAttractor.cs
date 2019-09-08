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
    /**
     * Helper struct to pass return values.
     */
    public class SnapResult
    {
        public double SnapToValue { get; set; }
        public double Force { get; set; }
    }

    /**
     * This is called by the SnapHandler the attractor registered to.
     * 
     * should return null if not snapping
     */
    public interface IValueSnapAttractor
    {
        SnapResult CheckForSnap(double value);
    }
}
