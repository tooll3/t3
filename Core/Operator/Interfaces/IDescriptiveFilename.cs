using System.Collections.Generic;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator.Interfaces
{
    /// <summary>
    /// Provides information required to render additional information in Graph nodes 
    /// </summary>
    public interface IDescriptiveFilename
    {
        public IEnumerable<string> FileFilter { get; }
        InputSlot<string> SourcePathSlot { get; }
    }
}