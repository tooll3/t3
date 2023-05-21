using System;
using System.IO;
using T3.SystemUi.Logging;

namespace T3.Core.Logging
{
    /// <summary>
    /// Write Debug-Log messages to log files
    /// </summary>
    public class FileWriter : ILogWriter
    {
        public ILogEntry.EntryLevel Filter { get; set; }

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

        public void ProcessEntry(ILogEntry entry)
        {
            lock (_fileWriter)
            {
                try
                {
                    _fileWriter.Write("{0:HH:mm:ss.fff} ({1}): {2}", entry.TimeStamp, entry.Level.ToString(), entry.Message + "\n");
                }
                catch (Exception)
                {
                    // skip encoder exception
                }
            }
        }

        private readonly StreamWriter _fileWriter;

        public static ILogWriter CreateDefault()
        {
            Directory.CreateDirectory(@".t3\log");
            return new FileWriter($@".t3\log\{DateTime.Now:yyyy_MM_dd-HH_mm_ss_fff}.log")
                       {
                           Filter = ILogEntry.EntryLevel.All
                       };
        }
    }
}