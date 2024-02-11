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
            _streamWriter = new StreamWriter(filename);
            //#if DEBUG
            _streamWriter.AutoFlush = true;
            //#endif
        }

        public void Dispose()
        {
            _streamWriter.Flush();
            _streamWriter.Close();
            _streamWriter.Dispose();
        }

        public static void Flush()
        {
            if (Instance == null)
                return;
            
            lock (Instance._streamWriter)
            {
                Instance._streamWriter.Flush();
            }
        }

        public void ProcessEntry(ILogEntry entry)
        {
            lock (_streamWriter)
            {
                try
                {
                    _streamWriter.Write("{0:HH:mm:ss.fff} ({1}): {2}", entry.TimeStamp, entry.Level.ToString(), entry.Message + "\n");
                }
                catch (Exception)
                {
                    // skip encoder exception
                }
            }
        }

        private readonly StreamWriter _streamWriter;
        private static FileWriter? Instance { get; set; }

        public static ILogWriter CreateDefault(string rootDirectory)
        {
            var logDirectory = Path.Combine(rootDirectory, LogSubDirectory);
            Directory.CreateDirectory(logDirectory);
            if (Instance != null)
                return Instance;
            
            var path = Path.Combine(logDirectory, $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}.log");
            Instance = new FileWriter(path)
                           {
                               Filter = ILogEntry.EntryLevel.All
                           };
            return Instance;
        }
        
        public const string LogSubDirectory = "log";
    }
}