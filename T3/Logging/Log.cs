using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Threading;

namespace T3.Logging
{
    /// <summary>
    /// A singleton that allows to log messages that are forwarded to <see cref="ILogWriter"/>s.
    /// </summary>
    public class Log
    {
        #region API for setup
        public static void Initialize(Dispatcher dispatcher)
        {
            _instance._mainThreadDispatcher = dispatcher;
        }


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
        #endregion


        #region API for logging
        public static void Debug(String message)
        {
            AddEntry(new LogEntry(LogEntry.EntryLevel.Debug, message));
        }

        public static void Debug(String message, Guid sourceId)
        {
            AddEntry(new LogEntry(LogEntry.EntryLevel.Debug, sourceId, message));
        }

        public static void DebugFormat(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            AddEntry(new LogEntry(LogEntry.EntryLevel.Debug, messageString));
        }


        public static void InfoFormat(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            AddEntry(new LogEntry(LogEntry.EntryLevel.Info, messageString));
        }


        private const int DEFAULT_LINE_LENGTH = 100;
        static StringBuilder _accumulatedInfoLine = new StringBuilder(String.Empty, DEFAULT_LINE_LENGTH);
        public static void AccumulateAsInfoLine(String c, int lineLength = DEFAULT_LINE_LENGTH)
        {
            _accumulatedInfoLine.Append(c);
            if (_accumulatedInfoLine.Length > lineLength)
            {
                InfoFormat(_accumulatedInfoLine.ToString());
                _accumulatedInfoLine.Clear();
            }
        }

        public static void Warning(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            AddEntry(new LogEntry(LogEntry.EntryLevel.Warning, messageString));
        }


        public static void Error(String message, params object[] args)
        {
            var messageString = FormatMessageWithArguments(message, args);
            AddEntry(new LogEntry(LogEntry.EntryLevel.Error, messageString));
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
                AddEntry(new LogEntry(LogEntry.EntryLevel.Info, "Ignoring arguments mal-formated debug message. Did you mess with curly braces?"));
            }
            return messageString;
        }


        private static void LogDebug(LogEntry.EntryLevel level, String message)
        {
            AddEntry(new LogEntry(level, Guid.Empty, message));
        }

        private static void AddEntry(LogEntry entry)
        {
            if (_instance._mainThreadDispatcher == null || _instance._mainThreadDispatcher.CheckAccess())
            {
                DoLog(entry);
            }
            else
            {
                Action action = () => DoLog(entry);
                _instance._mainThreadDispatcher.BeginInvoke(action, DispatcherPriority.Background);
            }
        }

        private static void DoLog(LogEntry entry)
        {
            _instance._logWriters.ForEach(writer => writer.ProcessEntry(entry));
        }


        private Dispatcher _mainThreadDispatcher;
        private static Log _instance = new Log();
        private List<ILogWriter> _logWriters = new List<ILogWriter>();

        private Log() { }   // Prevent construction
    }
}
