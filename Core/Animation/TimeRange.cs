using System;

namespace T3.Core.Animation
{
    /// <summary>
    /// A helper struct to handle and operate with duration of elements
    /// </summary>
    public struct TimeRange
    {
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

        public static TimeRange Undefined = new TimeRange(float.PositiveInfinity, float.NegativeInfinity);

        public TimeRange(float start, float end)
        {
            Start = start;
            End = end;
        }

        public float Start;
        public float End;
        public float Duration => End - Start;
        public bool IsValid => !float.IsInfinity(Start) && !float.IsInfinity(End);

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

        public static bool operator !=(TimeRange a, TimeRange b) 
        {
            return Math.Abs(a.Start - b.Start) > 0.001 
                   | Math.Abs(a.End - b.End) > 0.001;
        }        
    }
}