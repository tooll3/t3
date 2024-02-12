using System.Runtime.InteropServices;
using T3.Core.IO;

namespace lib.Utils;

public class TapProvider : ITapProvider
{
    public static readonly TapProvider Instance = new();

    private TapProvider()
    {
        if(Instance != null)
            throw new Exception("TapProvider is a singleton and should not be instantiated more than once");
    }
    
    public bool BeatTapTriggered { get; set; }
    public bool ResyncTriggered { get; set; }
    public float SlideSyncTime { get; set; }
}