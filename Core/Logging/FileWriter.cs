using System;
using System.IO;
using T3.Core.Logging;

namespace T3.Core.Logging
{
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
            lock (_fileWriter)
            {
                _fileWriter.Write("{0:HH:mm:ss.fff} ({1}): {2}", entry.TimeStamp, entry.Level.ToString(), entry.Message + "\n");
            }
        }

        private readonly StreamWriter _fileWriter;
    }
}
