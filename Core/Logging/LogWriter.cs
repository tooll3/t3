using System;
using System.IO;
using T3.Core.Logging;

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
            Console.Write("{0}: {1}", newEntry.Level.ToString(), newEntry.Message + "\n");
        }
    }

    /// <summary>
    /// Write Debug-Log messages to log files
    /// </summary>
    public class FileWriter : ILogWriter
    {
        public LogEntry.EntryLevel Filter { get; set; }

        public FileWriter(String filename)
        {
            _fileWriter = new StreamWriter(filename);
#if DEBUG
            _fileWriter.AutoFlush = true;
#endif
        }

        public void Dispose()
        {
            _fileWriter.Flush();
            _fileWriter.Close();
            _fileWriter.Dispose();
        }

        public void ProcessEntry(LogEntry entry)
        {
            _fileWriter.Write("{0} ({1}): {2}", entry.TimeStamp.ToString("HH:mm:ss.fff"), entry.Level.ToString(), entry.Message + "\n");
        }

        private readonly StreamWriter _fileWriter;
    }
}
