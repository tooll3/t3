using System;

namespace T3.Core.Animation
{
    public interface ITimeClip
    {
        Guid Id { get; }
        ref TimeRange TimeRange { get; }
        ref TimeRange SourceRange { get; }
        int LayerIndex { get; set; }
        string Name { get; }
    }
}