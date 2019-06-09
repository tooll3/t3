using System;

namespace T3.Core.Logging
{
    public interface ILogWriter : IDisposable
    {
        LogEntry.EntryLevel Filter { get; set; }
        void ProcessEntry(LogEntry entry);
    }
}
