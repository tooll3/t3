using System.Runtime.InteropServices;
using T3.Core.Resource;
// ReSharper disable EmptyConstructor

namespace lib;

// ReSharper disable once UnusedType.Global
public class ShareDefinition : IShareResources
{
    public ShareDefinition(){}
    #pragma warning disable CA1822
    public bool ShouldShareResources => true;
    #pragma warning restore CA1822
}