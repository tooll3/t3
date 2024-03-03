using T3.SystemUi.Logging;

namespace T3.Core.Logging;

public interface ILogWriter : IDisposable
{
    ILogEntry.EntryLevel Filter { get; set; }
    void ProcessEntry(ILogEntry entry);
}