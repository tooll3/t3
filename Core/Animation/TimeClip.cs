using System;

namespace T3.Core.Animation
{
    public class TimeClip
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public TimeRange VisibleRange;
        public TimeRange SourceRange;
        public int LayerIndex { get; set; }
    }
}