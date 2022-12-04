using System;

namespace T3.Core.Logging
{
    public class LogEntry
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

        public DateTime TimeStamp { get; }
        public EntryLevel Level { get; }
        public string Message { get; }
        public Guid[] SourceIdPath  { get; }

        public LogEntry(EntryLevel level, string message, Guid[] sourceIdPath)
        {
            TimeStamp = DateTime.Now;
            Level = level;
            Message = message;
            SourceIdPath = sourceIdPath;
        }
        
        public LogEntry(EntryLevel level, string message, Guid sourceId)
        {
            TimeStamp = DateTime.Now;
            Level = level;
            Message = message;
            SourceIdPath = new []{sourceId};
        }

        public LogEntry(EntryLevel level, string message)
        {
            TimeStamp = DateTime.Now;
            Level = level;
            Message = message;
            SourceIdPath = null;
        }
        
        public double SecondsSinceStart => (TimeStamp - _startTime).TotalSeconds;
        public double SecondsAgo => (DateTime.Now - TimeStamp).TotalSeconds;
        public Guid SourceId => SourceIdPath is { Length: > 0 } ? SourceIdPath[^1] : Guid.Empty;
        private static readonly DateTime _startTime = DateTime.Now;
    }
}
