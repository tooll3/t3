using ImGuiNET;
using SilkWindows.Implementations.FileManager.ItemDrawers;

namespace SilkWindows.Implementations.FileManager;

public sealed partial class FileManager
{
    public void OnFileDrop(string[] filePaths)
    {
        _droppedPaths = filePaths.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
    
    public bool IsDraggingPaths => _isDraggingMouse && _draggedPaths.Length > 0;
    private bool _isDraggingMouse;
    
    bool IFileManager.IsDropTarget(FileSystemDrawer drawer)
    {
        if (drawer.IsReadOnly)
            return false;
        
        if (_draggedPaths.Length == 0)
        {
            Console.WriteLine("No dragged files");
            return false;
        }
        
        return drawer switch
                   {
                       DirectoryDrawer directoryDrawer => CanReceiveDraggedFiles(directoryDrawer, _draggedPaths),
                       FileDrawer fileDrawer           => CanReceiveDraggedFiles(fileDrawer.ParentDirectoryDrawer!, _draggedPaths),
                       _                               => throw new ArgumentOutOfRangeException(nameof(drawer), drawer, null)
                   };
        
        static bool CanReceiveDraggedFiles(DirectoryDrawer directoryDrawer, string[] draggedPaths)
        {
            var targetDirectory = directoryDrawer.DirectoryInfo.FullName;
            foreach (var path in draggedPaths)
            {
                if (path == targetDirectory)
                    return false;
                
                var incompatibleFilePath = Path.Combine(targetDirectory, Path.GetFileName(path));
                if (path == incompatibleFilePath)
                {
                    Console.WriteLine($"Can't receive dropped files from its own directory.\nSource: \"{path}\nTarget: \"{targetDirectory}\"");
                    return false;
                }
            }
            
            return true;
        }
    }
    
    void IFileManager.ConsumeDroppedFiles(FileSystemDrawer drawer)
    {
        if(_droppedPaths.Length == 0)
            throw new InvalidOperationException("Files were already consumed");
        
        var directoryDrawer = drawer switch
                                  {
                                      DirectoryDrawer dir   => dir,
                                      FileDrawer fileDrawer => fileDrawer.ParentDirectoryDrawer!,
                                      _                     => throw new ArgumentOutOfRangeException(nameof(drawer), drawer, null)
                                  };
        
        var droppedPaths = _droppedPaths;
        NewEmptyArray(ref _droppedPaths);
        
        if (directoryDrawer.IsReadOnly)
        {
            return;
        }
        
        if (TryDropPathsInto(directoryDrawer.RootDirectory, directoryDrawer.DirectoryInfo, droppedPaths))
        {
            directoryDrawer.MarkNeedsRescan();
        }
    }
    
    private bool TryDropPathsInto(DirectoryInfo rootDirectory, DirectoryInfo targetDirectory, string[] paths)
    {
        if (paths.Length == 0)
            return false;
        var targetRootDirectoryPath = rootDirectory.FullName;
        var droppedDirectories = new List<DirectoryInfo>();
        var droppedFiles = new List<FileInfo>();
        
        foreach (var path in paths)
        {
            var attributes = File.GetAttributes(path);
            
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                var directory = new DirectoryInfo(path);
                
                if (!directory.Exists)
                {
                    Console.WriteLine("Directory not found: " + path);
                    continue;
                }
                
                droppedDirectories.Add(directory);
            }
            else
            {
                var file = new FileInfo(path);
                
                if (!file.Exists)
                {
                    Console.WriteLine("File not found: " + path);
                    continue;
                }
                
                droppedFiles.Add(file);
            }
        }
        
        // remove any duplicate directories
        for (var i = droppedDirectories.Count - 1; i >= 0; i--)
        {
            var directory = droppedDirectories[i];
            bool shouldRemove = false;
            
            foreach (var otherDirectory in droppedDirectories)
            {
                if (otherDirectory == directory)
                    continue;
                
                if (otherDirectory.FullName.StartsWith(directory.FullName))
                {
                    shouldRemove = true;
                    break;
                }
            }
            
            if (shouldRemove)
            {
                droppedDirectories.RemoveAt(i);
            }
        }
        
        // remove any duplicate or unwanted files
        var files = droppedFiles
                   .Where(file => _fileFilter(file.FullName))
                   .Where(x => !droppedDirectories
                               .Select(dir => dir.FullName)
                               .Any(x.FullName.StartsWith))
                   .ToArray();
        
        var movedOne = false;
        foreach (var file in files)
        {
            var moveType = file.FullName.StartsWith(targetRootDirectoryPath) ? FileOperations.MoveType.Move : FileOperations.MoveType.Copy;
            movedOne |= FileOperations.TryMoveFile(moveType, file, targetDirectory, _fileConflictResolver);
        }
        
        foreach (var directory in droppedDirectories)
        {
            var moveType = directory.FullName.StartsWith(targetRootDirectoryPath) ? FileOperations.MoveType.Move : FileOperations.MoveType.Copy;
            movedOne |= FileOperations.TryMoveDirectory(moveType, directory, targetDirectory, _fileConflictResolver);
        }
        
        return movedOne;
    }
    
    private void HandleFileDragEvents()
    {
        var isDragging = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
        if (isDragging != _isDraggingMouse)
        {
            switch (isDragging)
            {
                case true: // start dragging
                    _isDraggingMouse = true;
                    _draggedPaths = _selections.Select(x => x.Path).ToArray();
                    break;
                case false: // stop dragging
                    _isDraggingMouse = false;
                    _droppedPaths = _draggedPaths;
                    NewEmptyArray(ref _draggedPaths);
                    break;
            }
        }
        else if (!isDragging)
        {
            // clear dropped files if we are not dragging files after a frame of no dragging
            // todo - this likely breaks external drag and drop
            NewEmptyArray(ref _droppedPaths);
        }
    }
    
    private void DragFileDragIndicators()
    {
        var isDragging = IsDraggingPaths;
        ImGui.SetMouseCursor(isDragging ? ImGuiMouseCursor.Hand : ImGuiMouseCursor.Arrow);
        if (!IsDraggingPaths) return;
        
        ImGui.BeginTooltip();
        
        foreach (var item in _selections)
        {
            ImGui.Text(item.Name);
            if (item.IsDirectory)
            {
                ImGui.SameLine();
                ImGui.Text("/");
            }
        }
        
        ImGui.EndTooltip();
    }
    
    private string[] _droppedPaths = [];
    private string[] _draggedPaths = [];
    public bool HasDroppedFiles => _droppedPaths.Length > 0;
}