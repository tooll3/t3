using System;
using System.Collections.Generic;
using T3.Core.Logging;

namespace T3.Gui.Interaction.Snapping
{
    public class ValueSnapHandler
    {
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

//        public class SnapEventArgs : EventArgs
//        {
//            public double Value { get; set; }
//        }
        
        /// <summary>
        /// Components can bind to these events to render snap-indicators
        /// </summary>
        public event Action<double> SnappedEvent;
        
        
        /// <summary>
        /// Uses all registered snap providers to test for snapping
        /// </summary>
        /// <returns>snap target of NaN</returns>
        public double CheckForSnapping(double time, List<IValueSnapAttractor> ignoreSnapAttractors= null)
        {
            var bestSnapValue = Double.NaN;
            double maxSnapForce = 0;
            foreach (var sp in _snapAttractors)
            {
                if (ignoreSnapAttractors != null && ignoreSnapAttractors.Contains(sp)) 
                    continue;
                
                var snapResult = sp.CheckForSnap(time);
                if (snapResult != null && snapResult.Force > maxSnapForce)
                {
                    bestSnapValue = snapResult.SnapToValue;
                    maxSnapForce = snapResult.Force;
                }
            }
            
            if (!double.IsNaN(bestSnapValue))
            {
                SnappedEvent?.Invoke(bestSnapValue);
            }
            
            return bestSnapValue;
        }

        public double CheckForSnapping(double time, IValueSnapAttractor ignoreSnapAttractor)
        {
            var list = new List<IValueSnapAttractor> { ignoreSnapAttractor };
            return CheckForSnapping(time, list);
        }

        private readonly List<IValueSnapAttractor> _snapAttractors = new List<IValueSnapAttractor>();
    }
}
