using System;

namespace T3.Gui.Windows.TimeLine
 {
     /// <summary>
     /// A helper struct to handle and operate with duration of elements
     /// </summary>
     public struct TimeRange
     {
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
     }
 }