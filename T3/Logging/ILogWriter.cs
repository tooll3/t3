using System;

namespace T3.Logging
{
    public interface ILogWriter : IDisposable
    {
        LogEntry.EntryLevel Filter { get; set; }
        void ProcessEntry(LogEntry entry);
    }
}
