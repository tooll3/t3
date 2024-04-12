using System.Collections.Concurrent;
using System.Numerics;
using ImGuiNET;
using SilkWindows.Implementations.FileManager.ItemDrawers;
using T3.Core.SystemUi;

namespace SilkWindows.Implementations.FileManager;

public readonly record struct ManagedDirectory(string Path, bool IsReadOnly, string? Alias = null);

public enum FileManagerMode
{
    PickDirectory,
    PickFile,
    Manage
}

public sealed partial class FileManager : IImguiDrawer<string>, IFileManager
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
    
    public void OnRender(string windowName, double deltaSeconds, ImFonts fonts)
    {
        switch (_mode)
        {
            case FileManagerMode.PickDirectory:
                DrawPickButtonAndSetResult<DirectoryDrawer>("Pick directory");
                break;
            case FileManagerMode.PickFile:
                DrawPickButtonAndSetResult<FileDrawer>("Pick file");
                break;
            case FileManagerMode.Manage:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        ImGui.SameLine();
        ImGui.Text("\tDragged files: " + _draggedPaths.Length);
        HandleFileDragEvents();
        DragFileDragIndicators();
        
        ImGui.SameLine();
        ImGui.Text("\tMouse dragging: " + _isDraggingMouse);
        ImGui.SameLine();
        ImGui.Text("\tSelection count: " + _selections.Count);
        
        const ImGuiTableFlags tableFlags = ImGuiTableFlags.Reorderable;
        if (ImGui.BeginTable(_uniqueIdSuffix, _directoryDrawers.Length, tableFlags))
        {
            ImGui.TableNextRow();
            for (var index = 0; index < _directoryDrawers.Length; index++)
            {
                var directoryDrawer = _directoryDrawers[index];
                var columnId = "##col_" + directoryDrawer.Path;
                ImGui.TableSetupColumn(columnId, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetColumnIndex(index);
                
                // draw header
                var tableHeaderTitle = $"{(directoryDrawer.IsReadOnly ? "[Read Only]" : string.Empty)} ";
                ImGui.TableHeader(tableHeaderTitle);
                
                // draw separator - hack?
                var style = ImGui.GetStyle();
                var thickness = style.CellPadding.X;
                var headerMin = ImGui.GetItemRectMin();
                var headerMax = ImGui.GetItemRectMax();
                var minPosition = headerMin with { X = headerMin.X - thickness };
                var maxPosition = headerMax with { X = minPosition.X + thickness };
                var drawList = ImGui.GetWindowDrawList();
                var windowColor = ImGui.GetColorU32(ImGuiCol.WindowBg);
                drawList.AddRectFilled(minPosition, maxPosition, windowColor);
            }
            
            ImGui.TableNextRow();
            for (var index = 0; index < _directoryDrawers.Length; index++)
            {
                var directoryDrawer = _directoryDrawers[index];
                ImGui.TableSetColumnIndex(index);
                
                // we do this so we can have the same directory potentially in multiple columns commander-style
                //var childId = index +  + _uniqueIdSuffix + "##" + index;
                ImGui.BeginChild(directoryDrawer.Path);
                
                // for being able to drag and drop across columns
                //if (_isDraggingMouse && ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                //ImGui.SetWindowFocus();
                
                directoryDrawer.Draw(fonts);
                
                ImGui.EndChild();
            }
            
            ImGui.EndTable();
        }
        
        
        
        return;
        
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
    
    private void DrawPickButtonAndSetResult<T>(string buttonName) where T : FileSystemDrawer
    {
        var first = _selections.FirstOrDefault() as T;
        var isEnabled = first is not null;
        
        if (!isEnabled)
        {
            ImGui.BeginDisabled();
        }
        
        if (ImGui.Button(buttonName))
        {
            Result = first!.Path;
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
    public void ItemClicked(FileSystemDrawer drawer)
    {
        var wasSelected = IsSelected(drawer);
        
        var ctrl = ImGui.GetIO().KeyCtrl;
        // todo - shift / ctrl to select/deselect multiple
        
        if (!ctrl)
            _selections.Clear();
        else if (wasSelected)
            return;
        
        _selections.Add(drawer);
    }
    
    public void RemoveFromSelection(FileSystemDrawer drawer)
    {
        _selections.Remove(drawer);
    }
    
    public bool IsSelected(FileSystemDrawer drawer)
    {
        return _selections.Contains(drawer);
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
    
    void IFileManager.ShowInSystemFileManager(FileSystemDrawer drawer)
    {
        // todo: open in system file manager
        var directory = drawer.IsDirectory ? drawer.Path : drawer.ParentDirectoryDrawer!.Path;
        
        Log(drawer, $"Showing in system file manager");
    }
    
    public void Log(FileSystemDrawer drawer, string log)
    {
        var parent = drawer.ParentDirectoryDrawer;
        var fileOrDirectory = drawer.IsDirectory ? "Directory " : "File ";
        var msg = parent != null
                      ? fileOrDirectory + $"[..{parent.Name}/{drawer.Name}]: {log}"
                      : $"[{drawer.Name}]: {log}";
        
        Log(msg);
    }
    
    void IFileManager.CreateNewSubfolder(DirectoryDrawer directoryDrawer, bool consumeDroppedFiles)
    {
        var nameFolderDialog = new NewSubfolderWindow(directoryDrawer.DirectoryInfo);
        var result = ImguiWindowService.Instance.Show("Create new subfolder", nameFolderDialog);
        
        if (result is not { Exists: true })
        {
            if (consumeDroppedFiles)
                ConsumeArray(ref _droppedPaths);
            
            return;
        }
        
        if (consumeDroppedFiles)
        {
            if (_droppedPaths.Length == 0)
            {
                Log("No dropped files to consume??");
                return;
            }
            
            _ = TryDropPathsInto(directoryDrawer.RootDirectory, result, _droppedPaths);
            
            ConsumeArray(ref _droppedPaths);
        }
        
        directoryDrawer.MarkNeedsRescan();
    }
    
    public string FormatPathForDisplay(string path)
    {
        return path.Replace('\\', '/');
    }
    
    // ReSharper disable once RedundantAssignment
    // a silly little helper method to keep the way I'm handling the dragged/dropped files consistent and easy to find
    private static void ConsumeArray<T>(ref T[] paths) => paths = [];
    
    private void Log(string log) => _logs.Enqueue(log);
    
    private static readonly HashSet<FileSystemDrawer> _selections = [];
    
    private static int _fileManagerCount = 0;
    private readonly string _uniqueIdSuffix = "##" + Interlocked.Increment(ref _fileManagerCount);
    
    private readonly FileManagerMode _mode;
    private readonly ConcurrentQueue<string> _logs = new();
    private const int MaxLogCount = 100;
    private readonly Func<string, bool> _fileFilter;
    private bool _shouldClose = false;
    private readonly DirectoryDrawer[] _directoryDrawers;
    public string? Result { get; private set; }
    private bool _multiSelectEnabled;
}