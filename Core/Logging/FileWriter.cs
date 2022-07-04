using System;
using System.IO;
using T3.Core.Logging;

namespace Core.Logging
{
    /// <summary>
    /// Write Debug-Log messages to log files
    /// </summary>
    public class FileWriter : ILogWriter
    {
        public LogEntry.EntryLevel Filter { get; set; }

        public FileWriter(string filename)
        {
            _fileWriter = new StreamWriter(filename);
//#if DEBUG
            _fileWriter.AutoFlush = true;
//#endif
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

        public static ILogWriter CreateDefault()
        {
            Directory.CreateDirectory(@"Log");
            return new FileWriter($@"Log/{DateTime.Now:yyyy_MM_dd-HH_mm_ss_fff}.log")
                       {
                           Filter = LogEntry.EntryLevel.All
                       };
        }
    }
}
