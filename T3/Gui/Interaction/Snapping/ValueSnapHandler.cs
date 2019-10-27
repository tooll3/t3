using System;
using System.Collections.Generic;

namespace T3.Gui.Interaction.Snapping
{
    public class ValueSnapHandler
    {
        readonly List<IValueSnapAttractor> _snapAttractors = new List<IValueSnapAttractor>();

        public void AddSnapAttractor(IValueSnapAttractor sp)
        {
            if (!_snapAttractors.Contains(sp))
            {
                _snapAttractors.Add(sp);
            }
        }

        public void RemoveSnapAttractor(IValueSnapAttractor sp)
        {
            if (_snapAttractors.Contains(sp))
            {
                _snapAttractors.Remove(sp);
            }
        }

        public class SnapEventArgs : EventArgs
        {
            public double Value { get; set; }
        }
        
        /// <summary>
        /// Components can bind to these events to render snap-indicators
        /// </summary>
        public event EventHandler<SnapEventArgs> SnappedEvent;
        public event EventHandler<SnapEventArgs> NotSnappedEvent;

        public double CheckForSnapping(double time, List<IValueSnapAttractor> ignoreSnapAttractors)
        {
            double bestSnapValue = Double.NaN;
            double maxSnapForce = 0;
            foreach (var sp in _snapAttractors)
            {
                if (!ignoreSnapAttractors.Contains(sp))
                {
                    var snapResult = sp.CheckForSnap(time);
                    if (snapResult != null && snapResult.Force > maxSnapForce)
                    {
                        bestSnapValue = snapResult.SnapToValue;
                        maxSnapForce = snapResult.Force;
                    }
                }
            }
            if (!Double.IsNaN(bestSnapValue))
            {
                if (SnappedEvent != null)
                    SnappedEvent(this, new SnapEventArgs() { Value = bestSnapValue });
            }
            else
            {
                if (NotSnappedEvent != null)
                    SnappedEvent(this, new SnapEventArgs() { Value = bestSnapValue });
            }
            return bestSnapValue;
        }

        public double CheckForSnapping(double time, IValueSnapAttractor ignoreSnapAttractor)
        {
            var list = new List<IValueSnapAttractor>();
            list.Add(ignoreSnapAttractor);
            return CheckForSnapping(time, list);
        }


        public double CheckForSnapping(double time)
        {
            return CheckForSnapping(time, new List<IValueSnapAttractor>());
        }
    }
}
