using System;
using System.Collections.Generic;

namespace T3.Core.Animation;

public interface ITimeClip
{
    /// <summary>
    /// Matches   SymbolChildId with the composition 
    /// </summary>
    /// <remarks>
    /// This leads to the potential issue of id-conflicts when multiple Outputs of a SymbolChild are timeclips.
    /// ToDo: This should be prevented in UI when adding new outputs.
    /// </remarks>
    Guid Id { get; }
        
    ref TimeRange TimeRange { get; }
    ref TimeRange SourceRange { get; }
    int LayerIndex { get; set; }
        
    public bool IsClipOverlappingOthers( IEnumerable<ITimeClip> allTimeClips)
    {
        foreach (var otherClip in allTimeClips)
        {
            if (otherClip == this)
                continue;

            if (LayerIndex != otherClip.LayerIndex)
                continue;


            var start = TimeRange.Start;
            var end = TimeRange.End;
            var otherStart = otherClip.TimeRange.Start;
            var otherEnd = otherClip.TimeRange.End;
                
            if (otherEnd <= start || otherStart >= end)
                continue;
                 
            return true;
        }

        return false;
    }
}