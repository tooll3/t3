#nullable enable
using T3.Core.Operator.Slots;
using Texture2D = T3.Core.DataTypes.Texture2D;

namespace T3.Core.Resource;

public sealed partial class ResourceManager
{
    /* TODO, ResourceUsage usage, BindFlags bindFlags, CpuAccessFlags cpuAccessFlags, ResourceOptionFlags miscFlags, int loadFlags*/
    public static Resource<Texture2D> CreateTextureResource(string relativePath, IResourceConsumer? instance)
    {
        return new Resource<Texture2D>(relativePath, instance, TryCreateTextureResourceFromFile);
    }
    
    public static Resource<Texture2D> CreateTextureResource(InputSlot<string> slot)
    {
        return new Resource<Texture2D>(slot, TryCreateTextureResourceFromFile);
    }
}