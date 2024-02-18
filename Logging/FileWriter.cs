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


        public static ILogWriter CreateDefault(string rootDirectory)
        {
            if (Instance != null)
                return Instance;
            
            LogDirectory = Path.Combine(rootDirectory, LogSubDirectory);
            Directory.CreateDirectory(LogDirectory);
            
            var path = Path.Combine(LogDirectory, $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}.log");
            Instance = new FileWriter(path)
                           {
                               Filter = ILogEntry.EntryLevel.All
                           };
            return Instance;
        }
        
        private readonly StreamWriter _streamWriter;
        public static string LogDirectory { get; private set; }
        private static FileWriter? Instance { get; set; }
        private const string LogSubDirectory = "log";
    }
}