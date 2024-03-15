#nullable enable
namespace T3.Core.Resource;

public interface IResourceContainer
{
    public string ResourcesFolder { get; }
    public ResourceFileWatcher? FileWatcher { get; }
}