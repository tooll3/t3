using System.Collections.Generic;
using System.Runtime.CompilerServices;
using T3.Core.Logging;

namespace T3.Core.Stats
{
    /// <summary>
    /// Performance profiling helper that counts slot updates per frame
    /// </summary>
    public class OpUpdateCounter : IRenderStatsProvider
    {
        public OpUpdateCounter()
        {
            if (!_registered)
            {
                RenderStatsCollector.RegisterProvider(this);
                _registered = true;
            }
        }
        
        public IEnumerable<(string, int)> GetStats()
        {
            yield return ("Slots", _updateCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CountUp()
        {
            _updateCount++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void StartNewFrame()
        {
            _updateCount=0;
        }

        private static int _updateCount;
        private static bool _registered;
    }
}