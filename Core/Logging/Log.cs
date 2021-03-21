using System;
using System.Collections.Generic;
using System.Text;
//using System.Windows.Threading;

namespace T3.Core.Logging
{
    /// <summary>
    /// A singleton that allows to log messages that are forwarded to <see cref="ILogWriter"/>s.
    /// </summary>
    public class Log
    {
        public static void Dispose()
        {
            foreach (var w in _instance._logWriters)
            {
                w.Dispose();
            }
        }

        public static void AddWriter(ILogWriter writer)
        {
            _instance._logWriters.Add(writer);
        }

        #region API for logging
        public static void Debug(string message)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Debug, message));
        }

        public static void Debug(string message, Guid sourceId)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Debug, sourceId, message));
        }

        public static void DebugFormat(string message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            DoLog(new LogEntry(LogEntry.EntryLevel.Debug, messageString));
        }

        public static void Info(string message)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Info, message));
        }

        public static void Info(string message, Guid sourceId)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Info, sourceId, message));
        }

        public static void InfoFormat(string message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            DoLog(new LogEntry(LogEntry.EntryLevel.Info, messageString));
        }


        public static void Warning(string message)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Warning, message));
        }

        public static void Warning(string message, Guid sourceId)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Warning, sourceId, message));
        }

        // public static void WarningFormat(string message, params object[] args)
        // {
        //     var messageString = FormatMessageWithArguments(message, args);
        //     DoLog(new LogEntry(LogEntry.EntryLevel.Warning, messageString));
        // }

        public static void Error(string message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            DoLog(new LogEntry(LogEntry.EntryLevel.Error, messageString));
        }

        public static void Assert(string message)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Warning, message));
        }
        
        public static void Assert(string message, Guid sourceId)
        {
            DoLog(new LogEntry(LogEntry.EntryLevel.Warning, sourceId, message));
        }
        
        private const int DEFAULT_LINE_LENGTH = 100;
        private static readonly StringBuilder _accumulatedInfoLine = new StringBuilder(String.Empty, DEFAULT_LINE_LENGTH);
        public static void AccumulateAsInfoLine(String c, int lineLength = DEFAULT_LINE_LENGTH)
        {
            _accumulatedInfoLine.Append(c);
            if (_accumulatedInfoLine.Length > lineLength)
            {
                InfoFormat(_accumulatedInfoLine.ToString());
                _accumulatedInfoLine.Clear();
            }
        }

        #endregion

        private static string FormatMessageWithArguments(string messageString, object[] args)
        {
            try
            {
                messageString = args.Length == 0 ? messageString : String.Format(messageString, args);
            }
            catch (FormatException)
            {
                DoLog(new LogEntry(LogEntry.EntryLevel.Info, "Ignoring arguments mal-formated debug message. Did you mess with curly braces?"));
            }
            return messageString;
        }

        // private static void LogDebug(LogEntry.EntryLevel level, String message)
        // {
        //     DoLog(new LogEntry(level, Guid.Empty, message));
        // }

        private static void DoLog(LogEntry entry)
        {
            _instance._logWriters.ForEach(writer => writer.ProcessEntry(entry));
        }



        private static readonly Log _instance = new Log();
        private readonly List<ILogWriter> _logWriters = new List<ILogWriter>();

        private Log() { }   // Prevent construction
    }
}
