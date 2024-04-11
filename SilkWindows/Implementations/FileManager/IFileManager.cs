namespace SilkWindows.Implementations.FileManager;

internal interface IFileManager
{
    public bool HasDroppedFiles { get; }
    public void ConsumeDroppedFiles(DirectoryDrawer directoryDrawer);
    
    public void ItemClicked(IFileSystemDrawer fileSystemInfo);
    public void RemoveFromSelection(IFileSystemDrawer fileSystemInfo);
    
    public bool IsSelected(IFileSystemDrawer fileSystemInfo);
    public bool IsSelectedOrParentSelected(IFileSystemDrawer fileSystemInfo);
    public void ClearSelection();
    public IEnumerable<FileSystemInfo> GetDirectoryContents(DirectoryInfo directory);
    public void DoubleClicked(FileDrawer file, bool inExternalEditor);
    
    public void ShowInSystemFileManager(IFileSystemDrawer drawer);
    public void Log(IFileSystemDrawer drawer, string log);
}