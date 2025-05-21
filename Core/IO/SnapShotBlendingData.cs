using System;
using System.Collections.Generic;

namespace T3.Core.IO;

/// <summary>
/// Allows operators to forward snapshot control and blending events to the editor.
/// </summary>
/// <remarks>
/// This could also be a singleton similar to ITapProviders
/// </remarks>
public static class SnapShotBlendingData
{
    public static readonly Dictionary<Guid, BlendRequest> CompositionBlendRequests = [];

    public struct BlendRequest
    {
        public List<float> BlendWeights = [];
        public List<int> BlendIndices = [];
        public Guid SymbolId= Guid.Empty;

        public BlendRequest()
        {
        }
    }
}