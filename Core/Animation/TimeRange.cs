using System;
using T3.Core.Utils;

namespace T3.Core.Animation
{
    /// <summary>
    /// A helper struct to handle and operate with duration of elements
    /// </summary>
    public struct TimeRange
    {
        public TimeRange(float start, float end)
        {
            Start = start;
            End = end;
        }
        
        public static TimeRange FromStartAndDuration(float start, float duration)
        {
            return new TimeRange(start, start + duration);
        }
        
        
        public bool Equals(TimeRange other)
        {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object obj)
        {
            return obj is TimeRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public static TimeRange Undefined = new(float.PositiveInfinity, float.NegativeInfinity);



        public float Start;
        public float End;

        public float Duration
        {
            get => End - Start;
            set => End = Start + value;
        }
        
        public bool IsValid => !float.IsInfinity(Start) && !float.IsInfinity(End) && Duration > 0;

        public TimeRange Clone()
        {
            return new TimeRange(Start, End);
        }
        
        public void Unite(TimeRange timeRange)
        {
            Start = Math.Min(Start, timeRange.Start);
            End = Math.Max(End, timeRange.End);
        }

        public void Unite(float time)
        {
            Start = Math.Min(Start, time);
            End = Math.Max(End, time);
        }

        public bool Contains(double time)
        {
            return Start <= time && End >= time;
        }
        
        public static bool operator ==(TimeRange a, TimeRange b) 
        {
            return Math.Abs(a.Start - b.Start) < 0.001 
                   && Math.Abs(a.End - b.End) < 0.001;
        }
        
        public static TimeRange operator *(TimeRange range, float factor) 
        {
            return new TimeRange(range.Start * factor, range.End * factor);
        }

        public static bool operator !=(TimeRange a, TimeRange b) 
        {
            return Math.Abs(a.Start - b.Start) > 0.001 
                   | Math.Abs(a.End - b.End) > 0.001;
        }        
        
        public static TimeRange Lerp(TimeRange a, TimeRange b, float t) 
        {
            return new TimeRange( MathUtils.Lerp(a.Start, b.Start,t), MathUtils.Lerp(a.End, b.End,t));
        }

        
    }
}