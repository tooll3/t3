using System;
using System.Collections.Generic;

namespace T3.Core.Logging
{
    public struct LogEntry
    {
        [Flags]
        public enum EntryLevel
        {
            Debug = 1,
            Info = 2,
            Warning = 4,
            Error = 8,
            All = Debug | Info | Warning | Error
        }

        public DateTime TimeStamp { get; private set; }
        public EntryLevel Level { get; private set; }
        public String Message { get; private set; }
        public Guid SourceId { get; private set; }

        public LogEntry(EntryLevel level, Guid sourceId, String message)
        {
            TimeStamp = DateTime.Now;
            Level = level;
            SourceId = sourceId;
            Message = message;
        }

        public LogEntry(EntryLevel level, String message)
        {
            TimeStamp = DateTime.Now;
            Level = level;
            Message = message;
            SourceId = Guid.Empty;
        }

        public LogEntry(EntryLevel level, String message, DateTime timeStamp)
        {
            TimeStamp = timeStamp;
            Level = level;
            Message = message;
            SourceId = Guid.Empty;
        }

        /**
         * Special method to clone an existing entry with a new lineMessage.
         * This is required for implementing splitting multiline-messages
         */
        public LogEntry(LogEntry original, String lineMessage)
        {
            TimeStamp = original.TimeStamp;
            Level = original.Level;
            Message = lineMessage;
            SourceId = original.SourceId;
        }

        public List<LogEntry> SplitIntoSingleLineEntries()
        {
            var result = new List<LogEntry>();
            foreach (var line in Message.Replace("\r", "").Split('\n'))
            {
                result.Add(new LogEntry(this, line));
            }
            return result;
        }
        
        public double SecondsSinceStart => (TimeStamp - _startTime).TotalSeconds;
        public double SecondsAgo => (DateTime.Now - TimeStamp).TotalSeconds;

        private static readonly DateTime _startTime = DateTime.Now;
    }
}
