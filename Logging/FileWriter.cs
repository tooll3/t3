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

        public static ILogWriter CreateDefault()
        {
            
            Directory.CreateDirectory(LogDirectory);
            if (Instance != null)
                return Instance;
            
            Instance = new FileWriter($@".t3\log\{DateTime.Now:yyyy_MM_dd-HH_mm_ss_fff}.log")
                           {
                               Filter = ILogEntry.EntryLevel.All
                           };
            return Instance;
        }
        
        public const string LogDirectory = @".t3\log\";
    }
}