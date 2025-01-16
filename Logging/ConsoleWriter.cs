namespace T3.Core.Logging;

/// <summary>
/// Write log messages to system console
/// </summary>
public class ConsoleWriter : ILogWriter
{
    public ILogEntry.EntryLevel Filter { get; set; }

    public void Dispose()
    {
    }

    public void ProcessEntry(ILogEntry newEntry)
    {
        try
        {
            Console.Write("{0}: {1}\n", newEntry.Level.ToString(), newEntry.Message);
        }
        catch(Exception e)
        {
            Log.Debug("Failed to print log message to console: " + e.Message);
        }
    }
}