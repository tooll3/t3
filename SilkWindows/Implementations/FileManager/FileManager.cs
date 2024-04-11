using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using T3.Core.SystemUi;

namespace SilkWindows.Implementations.FileManager;

public readonly record struct ManagedDirectory(string Path, bool IsReadOnly, string? Alias = null);

public enum FileManagerMode
{
    PickDirectory,
    PickFile,
    Manage
}

public sealed class FileManager : IImguiDrawer<string>, IFileManager
{
    public FileManager(FileManagerMode mode, IEnumerable<ManagedDirectory> rootDirectories, Func<string, bool>? fileFilter = null)
    {
        _mode = mode;
        _fileConflictResolver = FileConflictWindow;
        
        _fileFilter = fileFilter ?? (_ => true);
        _directoryDrawers = rootDirectories.Select(dir =>
                                                   {
                                                       var directoryInfo = new DirectoryInfo(dir.Path);
                                                       if (!directoryInfo.Exists)
                                                           throw new DirectoryNotFoundException(directoryInfo.FullName);
                                                       return new DirectoryDrawer(this, directoryInfo, dir.IsReadOnly, null, dir.Alias);
                                                   }).ToArray();
    }
    
    public FileManager(FileManagerMode mode, ManagedDirectory rootDirectory, Func<string, bool>? fileFilter = null) : this(mode, [rootDirectory], fileFilter)
    {
    }
    
    public void Init()
    {
    }
    
    public void OnRender(string windowName, double deltaSeconds, ImFonts? fonts)
    {
        switch (_mode)
        {
            case FileManagerMode.PickDirectory:
                DrawPickButtonAndSetResult<DirectoryDrawer>();
                break;
            case FileManagerMode.PickFile:
                DrawPickButtonAndSetResult<FileDrawer>();
                break;
            case FileManagerMode.Manage:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        ImGui.BeginTable(_uniqueIdSuffix, _directoryDrawers.Length);
        ImGui.TableNextRow();
        int column = 0;
        foreach (var directoryDrawer in _directoryDrawers)
        {
            ImGui.TableSetColumnIndex(column++);
            ImGui.BeginChild(directoryDrawer.Path + _uniqueIdSuffix);
            directoryDrawer.Draw();
            ImGui.EndChild();
        }
        
        ImGui.EndTable();
        
        ImGui.SetNextWindowScroll(new Vector2(0f, float.MaxValue));
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        ImGui.BeginChild("Logs" + _uniqueIdSuffix);
        while (_logs.Count > MaxLogCount)
        {
            _logs.TryDequeue(out _);
        }
        
        foreach (var log in _logs)
        {
            ImGui.Text(log);
        }
        
        ImGui.EndChild();
    }
    
    private void DrawPickButtonAndSetResult<T>() where T : class, IFileSystemDrawer
    {
        var first = _selections.FirstOrDefault() as T;
        var isEnabled = _selections.Count == 1 && first != null;
        isEnabled = isEnabled && _selections.First() is T;
        
        if (isEnabled)
        {
            Result = first!.Path;
        }
        
        if (!isEnabled)
        {
            ImGui.BeginDisabled();
        }
        
        if (ImGui.Button("Pick Directory"))
        {
            _shouldClose = true;
        }
        
        if (!isEnabled)
        {
            ImGui.EndDisabled();
        }
    }
    
    public void OnWindowUpdate(double deltaSeconds, out bool shouldClose)
    {
        shouldClose = _shouldClose;
    }
    
    public void OnClose()
    {
    }
    
    #region Feature: Drag and Drop
    public void OnFileDrop(string[] filePaths)
    {
        _droppedFiles = filePaths.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
    }
    
    public bool TryGetDroppedFiles([NotNullWhen(true)] out string[]? droppedFiles)
    {
        droppedFiles = _droppedFiles;
        return _droppedFiles is { Length: > 0 };
    }
    
    void IFileManager.ConsumeDroppedFiles(DirectoryDrawer directoryDrawer)
    {
        if (directoryDrawer.IsReadOnly)
        {
            BlockingWindow.Instance.ShowMessageBox("Cannot drop files in a read-only directory.");
            return;
        }
        
        if (_droppedFiles == null)
        {
            throw new InvalidOperationException($"Tried to consume dropped files, but none were available. Was it already consumed?");
        }
        
        if (!TryDropPathsInto(directoryDrawer.RootDirectory, (DirectoryInfo)directoryDrawer.FileSystemInfo, _droppedFiles))
        {
            return;
        }
        
        _droppedFiles = null;
        directoryDrawer.MarkNeedsRescan();
        
        return;
        
        bool TryDropPathsInto(DirectoryInfo rootDirectory, DirectoryInfo targetDirectory, IEnumerable<string> paths)
        {
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
            
            foreach (var file in files)
            {
                var moveType = file.FullName.StartsWith(targetRootDirectoryPath) ? FileOperations.MoveType.Move : FileOperations.MoveType.Copy;
                FileOperations.TryMoveFile(moveType, file, targetDirectory, _fileConflictResolver);
            }
            
            foreach (var directory in droppedDirectories)
            {
                var moveType = directory.FullName.StartsWith(targetRootDirectoryPath) ? FileOperations.MoveType.Move : FileOperations.MoveType.Copy;
                FileOperations.TryMoveDirectory(moveType, directory, targetDirectory, _fileConflictResolver);
            }
            
            return true;
        }
    }
    #endregion
    
    private readonly Func<FileInfo, FileConflictOption> _fileConflictResolver;
    
    private FileConflictOption FileConflictWindow(FileInfo file)
    {
        FileConflictOption? result = BlockingWindow.Instance.ShowMessageBox($"File \"{file.FullName}\" already exists.\n\nDo you want to overwrite it?",
                                                                            "File already exists",
                                                                            val => val.ToString(),
                                                                            Enum.GetValues<FileConflictOption>());
        
        if (result.HasValue)
            return result.Value;
        
        Log($"Window closed without selection - skipping {file.FullName}");
        return FileConflictOption.Skip;
    }
    
    #region Feature: Selection
    public void ItemClicked(IFileSystemDrawer drawer)
    {
        var wasSelected = IsSelected(drawer);
        
        // todo - shift / ctrl to select/deselect multiple
        _selections.Clear();
        
        if (wasSelected)
        {
            return;
        }
        
        _selections.Add(drawer);
    }
    
    public void RemoveFromSelection(IFileSystemDrawer drawer)
    {
        _selections.Remove(drawer);
    }
    
    public bool IsSelected(IFileSystemDrawer drawer)
    {
        return _selections.Contains(drawer);
    }
    
    public bool IsSelectedOrParentSelected(IFileSystemDrawer drawer)
    {
        return IsSelected(drawer) || drawer.ParentDirectoryDrawer != null && IsSelectedOrParentSelected(drawer.ParentDirectoryDrawer);
    }
    
    public void ClearSelection()
    {
        _selections.Clear();
    }
    #endregion
    
    public void OnWindowFocusChanged(bool changedTo)
    {
    }
    
    IEnumerable<FileSystemInfo> IFileManager.GetDirectoryContents(DirectoryInfo directory)
    {
        return directory.EnumerateFileSystemInfos()
                        .Where(x => x is DirectoryInfo || x is FileInfo fileInfo && _fileFilter(fileInfo.FullName));
    }
    
    void IFileManager.DoubleClicked(FileDrawer drawer, bool inExternalEditor)
    {
        if (_multiSelectEnabled)
            return;
        
        if (_selections.Count != 1)
        {
            Log($"Cannot open {_selections.Count} files at once");
            return;
        }
        
        if (_selections.First() != drawer)
        {
            Log("Selection does not match double clicked item");
            ItemClicked(drawer); // todo: this is a temporary hack - probably not even necessary at time of writing
        }
        
        if (inExternalEditor)
        {
            // todo: open in external editor if not read-only
            // should not be able to enter this mode if directory is read-only
            return;
        }
        
        switch (_mode)
        {
            case FileManagerMode.PickDirectory:
                break;
            case FileManagerMode.PickFile:
                Result = drawer.Path;
                _shouldClose = true;
                
                break;
            case FileManagerMode.Manage:
                // todo: open in external editor if not read-only
                // should not be able to enter this mode if directory is read-only
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        // todo: mode - file picker, open in default app, etc
        var path = drawer.Path;
        
        Log(drawer, "Opening " + path);
    }
    
    void IFileManager.ShowInSystemFileManager(IFileSystemDrawer drawer)
    {
        // todo: open in system file manager
        var directory = drawer.IsDirectory ? drawer.Path : drawer.ParentDirectoryDrawer!.Path;
        
        Log(drawer, $"Showing in system file manager");
    }
    
    public void Log(IFileSystemDrawer drawer, string log)
    {
        var parent = drawer.ParentDirectoryDrawer;
        var fileOrDirectory = drawer.IsDirectory ? "Directory " : "File ";
        var msg = parent != null
                      ? fileOrDirectory + $"[..{parent.Name}/{drawer.Name}]: {log}"
                      : $"[{drawer.Name}]: {log}";
        
        Log(msg);
    }
    
    private void Log(string log) => _logs.Enqueue(log);
    
    private static HashSet<IFileSystemDrawer> _selections = new();
    
    private static int _fileManagerCount = 0;
    private readonly string _uniqueIdSuffix = "##" + Interlocked.Increment(ref _fileManagerCount);
    
    private readonly FileManagerMode _mode;
    private readonly ConcurrentQueue<string> _logs = new();
    private const int MaxLogCount = 100;
    private readonly Func<string, bool> _fileFilter;
    private string[]? _droppedFiles;
    private bool _shouldClose = false;
    private readonly DirectoryDrawer[] _directoryDrawers;
    public bool HasDroppedFiles => _droppedFiles != null;
    public string? Result { get; private set; }
    private bool _multiSelectEnabled;
}