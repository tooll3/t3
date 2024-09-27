using T3.SystemUi.Logging;

namespace T3.Core.Logging;

/// <summary>
/// Write Debug-Log messages to log files
/// </summary>
public class FileWriter : ILogWriter
{
    public ILogEntry.EntryLevel Filter { get; set; }

    public FileWriter(string directory, string filename)
    {
        LogDirectory = Path.Combine(directory, LogSubDirectory);
        _logPath = Path.Combine(LogDirectory, filename);
            
        Directory.CreateDirectory(LogDirectory);
        _streamWriter = new StreamWriter(_logPath);
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


    public static ILogWriter CreateDefault(string rootDirectory, out string path)
    {
        if (Instance != null)
        {
            path = Instance._logPath;
            return Instance;
        }

            
        var fileName = $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss_fff}.log";
        Instance = new FileWriter(rootDirectory, fileName)
                       {
                           Filter = ILogEntry.EntryLevel.All
                       };
            
        path = Instance._logPath;
        return Instance;
    }
        
    private readonly StreamWriter _streamWriter;
    private readonly string _logPath;
    public readonly string LogDirectory;
    public static FileWriter? Instance { get; private set; }
    private const string LogSubDirectory = "log";
}