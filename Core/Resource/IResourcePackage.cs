#nullable enable
namespace T3.Core.Resource;

public interface IResourcePackage
{
    public string ResourcesFolder { get; }
    public ResourceFileWatcher? FileWatcher { get; }
}