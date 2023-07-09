namespace T3.Core.Logging
{
    public interface IRenderStatsProvider
    {
        IEnumerable<(string, int)> GetStats();
        void StartNewFrame();
    }
}