namespace SilkWindows.Implementations.FileManager;

public interface IFileSystemDrawer
{
    public bool IsDirectory { get; }
    public string Name { get; }
    public string Path { get; }
    
    public FileSystemInfo FileSystemInfo { get; }
    
    public void Draw();
    
    public bool Expanded { get; set; }
    
    public IFileSystemDrawer? ParentDirectoryDrawer { get; }
}