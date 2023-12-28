namespace T3.Editor.Gui.Interaction.Timing;

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