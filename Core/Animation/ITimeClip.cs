using System;

namespace T3.Core.Animation
{
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
        string Name { get; }
    }
}