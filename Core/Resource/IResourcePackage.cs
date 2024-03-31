#nullable enable
namespace T3.Core.Resource;

public interface IResourcePackage
{
    public string? Alias { get; }
    public string ResourcesFolder { get; }
    public ResourceFileWatcher? FileWatcher { get; }
    public bool IsReadOnly { get; }
}