namespace T3.SystemUi.Logging;

public interface ILogEntry
{
    [Flags]
    public enum EntryLevel
    {
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
        All = Debug | Info | Warning | Error
    }

    DateTime TimeStamp { get; }
    EntryLevel Level { get; }
    string Message { get; }
    IList<Guid> SourceIdPath { get; }
    double SecondsSinceStart { get; }
    double SecondsAgo { get; }
    Guid SourceId { get; }
}