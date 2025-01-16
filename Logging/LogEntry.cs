namespace T3.Core.Logging;

internal sealed class LogEntry : ILogEntry
{
    public DateTime TimeStamp { get; }
    public ILogEntry.EntryLevel Level { get; }
    public string Message { get; }
    public IReadOnlyList<Guid> SourceIdPath { get; }

    internal LogEntry(ILogEntry.EntryLevel level, string message, IReadOnlyList<Guid> sourceIdPath)
    {
        TimeStamp = DateTime.Now;
        Level = level;
        Message = message;
        SourceIdPath = sourceIdPath;
    }

    internal LogEntry(ILogEntry.EntryLevel level, string message, Guid sourceId)
    {
        TimeStamp = DateTime.Now;
        Level = level;
        Message = message;
        SourceIdPath = new[] { sourceId };
    }

    internal LogEntry(ILogEntry.EntryLevel level, string message)
    {
        TimeStamp = DateTime.Now;
        Level = level;
        Message = message;
        SourceIdPath = _emptyPath;
    }

    public double SecondsSinceStart => (TimeStamp - _startTime).TotalSeconds;
    public double SecondsAgo => (DateTime.Now - TimeStamp).TotalSeconds;
    public Guid SourceId => SourceIdPath is { Count: > 0 } ? SourceIdPath[^1] : Guid.Empty;
    private static readonly DateTime _startTime = DateTime.Now;
    private static readonly List<Guid> _emptyPath = [];
}