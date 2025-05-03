using System.Collections.Generic;
using System.Runtime.CompilerServices;
using T3.Core.Logging;

namespace T3.Core.Stats;

/// <summary>
/// Performance profiling helper that counts slot updates per frame
/// </summary>
internal sealed class OpUpdateCounter : IRenderStatsProvider
{
    internal OpUpdateCounter()
    {
        RenderStatsCollector.RegisterProvider(this);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void CountUp()
    {
        _updateCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartNewFrame()
    {
        _updateCount=0;
    }
    
    IEnumerable<(string, int)> IRenderStatsProvider.GetStats()
    {
        yield return ("Slots", _updateCount);
    }

    private static int _updateCount;
}