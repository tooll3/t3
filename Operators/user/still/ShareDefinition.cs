using System.Runtime.InteropServices;
using T3.Core.Resource;
// ReSharper disable UnusedType.Global

namespace user.still;

public class ShareDefinition : IShareResources
{
    // ReSharper disable once EmptyConstructor
    public ShareDefinition(){}
    public bool ShouldShareResources => true;
}