namespace T3.SystemUi.Logging;

public interface ILogWriter : IDisposable
{
    ILogEntry.EntryLevel Filter { get; set; }
    void ProcessEntry(ILogEntry entry);
}