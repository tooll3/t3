using System.Runtime.InteropServices;
using T3.Core.Resource;

namespace user.pixtur;

// ReSharper disable once UnusedType.Global
public class ShareDefinition : IShareResources
{
    // ReSharper disable once EmptyConstructor
    public ShareDefinition(){}
    public bool ShouldShareResources => true;
}