
using System;

namespace T3.Core.Logging
{
    /// <summary>
    /// Write log messages to system console
    /// </summary>
    public class ConsoleWriter : ILogWriter
    {
        public LogEntry.EntryLevel Filter { get; set; }

        public void Dispose()
        {
        }

        public void ProcessEntry(LogEntry newEntry)
        {
            Console.Write("{0}: {1}\n", newEntry.Level.ToString(), newEntry.Message);
        }
    }
}
