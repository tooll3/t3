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
        

        /// <summary>
        /// Components can bind to these events to render snap-indicators
        /// </summary>
        public event Action<double> SnappedEvent;

        public bool CheckForSnapping(ref float time, List<IValueSnapAttractor> ignoreSnapAttractors = null)
        {
            double d = time;
            var result = CheckForSnapping(ref d, ignoreSnapAttractors);
            if (result)
                time = (float)d;

            return result;
        }

        /// <summary>
        /// Uses all registered snap providers to test for snapping
        /// </summary>
        public bool CheckForSnapping(ref double time, List<IValueSnapAttractor> ignoreSnapAttractors = null)
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

            if (double.IsNaN(bestSnapValue))
                return false;

            time = bestSnapValue;
            return true;
        }

        public bool CheckForSnapping(ref float time, IValueSnapAttractor ignoreSnapAttractor)
        {
            double d = time;
            var list = new List<IValueSnapAttractor> { ignoreSnapAttractor };
            var result = CheckForSnapping(ref d, list);
            if (result)
                time = (float)d;
            return result;
        }

        private readonly List<IValueSnapAttractor> _snapAttractors = new List<IValueSnapAttractor>();
    }
}