using System.Collections.Concurrent;
using System.Numerics;
using ImGuiNET;
using SilkWindows.Implementations.FileManager.ItemDrawers;
using T3.Core.SystemUi;

namespace SilkWindows.Implementations.FileManager;

public readonly record struct ManagedDirectory(string Path, bool IsReadOnly, bool startExpanded = true, string? Alias = null);

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
        _folderTabs = rootDirectories
                     .Select(dir =>
                             {
                                 var directoryInfo = new DirectoryInfo(dir.Path);
                                 if (!directoryInfo.Exists)
                                     throw new DirectoryNotFoundException(directoryInfo.FullName);
                                 
                                 var drawer = new DirectoryDrawer(this, directoryInfo, dir.IsReadOnly, null, dir.Alias);
                                 
                                 return new Column(drawer, dir.startExpanded);
                             }).ToArray();
    }
    
    public FileManager(FileManagerMode mode, ManagedDirectory rootDirectory, Func<string, bool>? fileFilter = null) : this(mode, [rootDirectory], fileFilter)
    {
    }
    
    public void Init()
    {
    }
    
    private class Column(DirectoryDrawer drawer, bool drawn)
    {
        public readonly DirectoryDrawer Drawer = drawer;
        public bool Drawn = drawn;
    }
    
    private readonly Column[] _folderTabs;
    private readonly List<Column> _columnsToDraw = [];
    private readonly List<Column> _columnsMinimized = [];
    
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
        
        if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
            _selectedRoot = null;
        
        _columnsToDraw.Clear();
        _columnsMinimized.Clear();
        
        foreach (var column in _folderTabs)
        {
            if (column.Drawn)
            {
                _columnsToDraw.Add(column);
            }
            else
            {
                if (_selectedRoot == column.Drawer)
                    _selectedRoot = null;
                
                _columnsMinimized.Add(column);
            }
        }
        
        if (_selectedRoot != null)
            Console.WriteLine("Selected root: " + _selectedRoot.DisplayName);
        
        if (_selectedRoot != null && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            // draw dragged indicator 
            Console.WriteLine("Dragging root");
            var displayName = _selectedRoot.DisplayName;
            ImGui.PushFont(fonts.Large);
            var size = GetButtonSize(displayName);
            ImGui.PopFont();
            
            var mousePos = ImGui.GetMousePos();
            
            var halfSize = size * 0.5f;
            var min = mousePos - halfSize;
            var max = mousePos + halfSize;
            var mid = (max + min) * 0.5f;
            
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.TableHeaderBg));
            
            var textStartPos = min + GetButtonInnerPadding();
            drawList.AddText(fonts.Large, fonts.Large.FontSize, textStartPos, ImGui.GetColorU32(ImGuiCol.Text), displayName);
        }
        
        CheckForFileDrop();
        DragFileDragIndicators(fonts);
        
        if (_columnsMinimized.Count > 0)
        {
            DrawCollapsedButtons(_columnsMinimized);
        }
        
        if (_columnsToDraw.Count > 0)
        {
            DrawTable(fonts, _columnsToDraw);
        }
        
        return;
        
        // todo - log toasts
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
    
    private void DrawCollapsedButtons(List<Column> collapsed)
    {
        ImGui.SameLine();
        
        // draw right-aligned buttons
        var startPosition = ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX();
        var style = ImGui.GetStyle();
        var innerSpacing = style.ItemInnerSpacing.X + (style.FramePadding.X * 2);
        foreach (var column in collapsed)
        {
            startPosition -= GetButtonSize(column.Drawer.DisplayName).X;
            //startPosition -= ImGui.CalcTextSize(column.Drawer.DisplayName).X + innerSpacing;
        }
        
        ImGui.SetCursorPosX(startPosition);
        
        foreach (var column in collapsed)
        {
            var drawer = column.Drawer;
            if (ImGui.Button(drawer.DisplayName + "##expand_" + drawer.Path))
            {
                column.Drawn = true;
            }
            
            ImGui.SameLine();
        }
        
        ImGui.NewLine();
    }
    
    private static Vector2 GetButtonSize(string text, bool useSpacing = true)
    {
        var style = ImGui.GetStyle();
        return ImGui.CalcTextSize(text) + style.ItemInnerSpacing * (useSpacing ? 1 : 0) + GetButtonInnerPadding();
    }
    
    private static Vector2 GetButtonInnerPadding() => ImGui.GetStyle().FramePadding * 2;
    
    private void DrawTable(ImFonts fonts, List<Column> expanded)
    {
        const ImGuiTableFlags tableFlags = ImGuiTableFlags.None;
        const ImGuiTableColumnFlags columnFlags = ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.NoHide |
                                                  ImGuiTableColumnFlags.NoSort;
        
        if (ImGui.BeginTable(_uniqueIdSuffix, expanded.Count, tableFlags))
        {
            ImGui.TableNextRow();
            for (var index = 0; index < expanded.Count; index++)
            {
                var column = expanded[index];
                var directoryDrawer = column.Drawer;
                var columnId = "##col_" + directoryDrawer.Path;
                
                ImGui.TableSetupColumn(columnId, columnFlags);
                ImGui.TableSetColumnIndex(index);
                
                // draw header
                if (ImGui.Button("_##minimize_" + directoryDrawer.Path))
                {
                    column.Drawn = false;
                }
                
                ImGui.SameLine();
                
                var tableHeaderTitle = $"{(directoryDrawer.IsReadOnly ? "[Read Only]" : string.Empty)} ";
                ImGui.TableHeader(tableHeaderTitle);
                
                // draw separator - hack?
                var headerMin = ImGui.GetItemRectMin();
                var headerMax = ImGui.GetItemRectMax();
                var style = ImGui.GetStyle();
                var thickness = style.CellPadding.X;
                var minPosition = headerMin with { X = headerMin.X - thickness };
                var maxPosition = headerMax with { X = minPosition.X + thickness };
                var drawList = ImGui.GetWindowDrawList();
                var windowColor = ImGui.GetColorU32(ImGuiCol.WindowBg);
                drawList.AddRectFilled(minPosition, maxPosition, windowColor);
            }
            
            ImGui.TableNextRow();
            for (var index = 0; index < expanded.Count; index++)
            {
                var column = expanded[index];
                var directoryDrawer = column.Drawer;
                ImGui.TableSetColumnIndex(index);
                
                ImGui.BeginChild(directoryDrawer.Path);
                
                directoryDrawer.Draw(fonts);
                
                ImGui.EndChild();
            }
            
            ImGui.EndTable();
        }
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
    
    public void OnWindowFocusChanged(bool changedTo)
    {
        ConsumeArray(ref _draggedPaths);
        ConsumeArray(ref _droppedPaths);
    }
    
    public void OnClose()
    {
    }
    
    
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
    
    #region IFileManager Implementation
    #region Feature: Selection
    public void ItemClicked(FileSystemDrawer drawer)
    {
        if (drawer is DirectoryDrawer { IsRoot: true } directoryDrawer)
        {
            _selectedRoot = directoryDrawer;
            _selections.Clear();
            return;
        }
        
        _selectedRoot = null;
        var wasSelected = IsSelected(drawer);
        
        var ctrl = ImGui.GetIO().KeyCtrl;
        // todo - shift to select/deselect multiple
        
        if (!ctrl)
            _selections.Clear();
        else if (wasSelected)
        {
            RemoveFromSelection(drawer);
            return;
        }
        
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
    
    IEnumerable<FileSystemInfo> IFileManager.GetDirectoryContents(DirectoryInfo directory)
    {
        return directory.EnumerateFileSystemInfos()
                        .Where(x => x is DirectoryInfo || x is FileInfo fileInfo && _fileFilter(fileInfo.FullName));
    }
    
    void IFileManager.DoubleClicked(FileDrawer drawer, bool inExternalEditor)
    {
        //if (_multiSelectEnabled)
          //  return;
        
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
                      ? fileOrDirectory + $"[..{parent.DisplayName}/{drawer.DisplayName}]: {log}"
                      : $"[{drawer.DisplayName}]: {log}";
        
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
    #endregion
    
    // ReSharper disable once RedundantAssignment
    // a silly little helper method to keep the way I'm handling the dragged/dropped files consistent and easy to find
    private static void ConsumeArray<T>(ref T[] paths) => paths = [];
    
    private void Log(string log) => _logs.Enqueue(log);
    
    private static readonly HashSet<FileSystemDrawer> _selections = [];
    
    private static DirectoryDrawer? _selectedRoot;
    
    private static int _fileManagerCount = 0;
    private readonly string _uniqueIdSuffix = "##" + Interlocked.Increment(ref _fileManagerCount);
    
    private readonly FileManagerMode _mode;
    private readonly ConcurrentQueue<string> _logs = new();
    private const int MaxLogCount = 100;
    private readonly Func<string, bool> _fileFilter;
    private bool _shouldClose;
    public string? Result { get; private set; }
    private readonly Func<FileInfo, FileConflictOption> _fileConflictResolver;
}