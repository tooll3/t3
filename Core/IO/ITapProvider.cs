namespace T3.Core.IO;

/// <summary>
/// Interfaces for forwarding information from lib operators to editor
/// </summary>
public interface ITapProvider
{
    public bool BeatTapTriggered { get; }
    public bool ResyncTriggered { get; }
    public float SlideSyncTime { get; }
}

public interface IBpmProvider
{
    public bool TryGetNewBpmRate(out float bpm);
}