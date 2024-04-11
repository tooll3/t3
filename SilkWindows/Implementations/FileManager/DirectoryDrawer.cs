using ImGuiNET;

namespace SilkWindows.Implementations.FileManager;

internal sealed class DirectoryDrawer : IFileSystemDrawer
{
    public string Name => _directory.Name;
    public string Path => _directory.FullName;
    public bool IsDirectory => true;
    public FileSystemInfo FileSystemInfo => _directory;
    public DirectoryInfo RootDirectory => _parent == null ? _directory : _parent.RootDirectory;
    public IFileSystemDrawer? ParentDirectoryDrawer => _parent;
    
    private readonly List<DirectoryDrawer> _directories = new();
    
    private readonly DirectoryDrawer? _parent;
    private readonly List<FileDrawer> _files = new();
    
    public bool Expanded { get; set; }
    
    private bool _needsRescan;
    private readonly IFileManager _fileManager;
    private readonly DirectoryInfo _directory;
    public readonly bool IsReadOnly;
    private readonly string _displayName;
    private readonly string _expandedButtonLabel;
    private readonly string _collapsedButtonLabel;
    
    public DirectoryDrawer(IFileManager fileManager, DirectoryInfo directory, bool isReadOnly, DirectoryDrawer? parent, string? alias = null)
    {
        _directory = directory;
        _needsRescan = true;
        _fileManager = fileManager;
        _parent = parent;
        IsReadOnly = isReadOnly;
        _displayName = string.IsNullOrEmpty(alias) ? _directory.Name : '/' + alias;
        var buttonIdSuffix = "##" + _displayName;
        _expandedButtonLabel = "[-]" + buttonIdSuffix;
        _collapsedButtonLabel = "[+]" + buttonIdSuffix;
    }
    
    public void MarkNeedsRescan() => _needsRescan = true;
    
    private void Rescan()
    {
        ClearChildren();
        
        var contents = _fileManager.GetDirectoryContents(_directory);
        foreach (var dir in contents)
        {
            switch (dir)
            {
                case DirectoryInfo di:
                    _directories.Add(new DirectoryDrawer(_fileManager, di, IsReadOnly, this));
                    break;
                case FileInfo fi:
                    _files.Add(new FileDrawer(_fileManager, fi, this));
                    break;
            }
        }
        
        return;
        
        void ClearChildren()
        {
            for (int i = _directories.Count - 1; i >= 0; i--)
            {
                _fileManager.RemoveFromSelection(_directories[i]);
                _directories.RemoveAt(i);
            }
            
            for (int i = _files.Count - 1; i >= 0; i--)
            {
                _fileManager.RemoveFromSelection(_files[i]);
                _files.RemoveAt(i);
            }
        }
    }
    
    public void DropInFiles()
    {
        _fileManager.ConsumeDroppedFiles(this);
    }
    
    public void Draw()
    {
        var expandCollapseLabel = Expanded ? _expandedButtonLabel : _collapsedButtonLabel;
        if (ImGui.Button(expandCollapseLabel))
        {
            Expanded = !Expanded;
        }
        
        ImGui.SameLine();
        
        if (ImGui.Selectable(_displayName, _fileManager.IsSelected(this)))
        {
            _fileManager.ItemClicked(this);
        }
        
        if (ImGui.IsItemHovered())
        {
            if (_fileManager.HasDroppedFiles)
            {
                DropInFiles();
            }
            
            if (ImGui.IsMouseDoubleClicked(0))
            {
                Expanded = !Expanded;
            }
            
            if (!IsReadOnly)
            {
                if (ImGui.BeginTooltip())
                {
                    ImGui.Text(_directory.FullName);
                    ImGui.EndTooltip();
                }
            }
        }
        
        if (ImGui.BeginPopupContextWindow())
        {
            if (ImGui.MenuItem("Refresh"))
            {
                MarkNeedsRescan();
            }
            
            ImGui.EndPopup();
        }
        
        if (!Expanded) return;
        
        if (_needsRescan)
        {
            Rescan();
            _needsRescan = false;
        }
        
        ImGui.Indent();
        ImGui.BeginGroup();
        foreach (var dir in _directories)
        {
            dir.Draw();
        }
        
        foreach (var file in _files)
        {
            file.Draw();
        }
        
        if (ImGui.Selectable("*New Subfolder*"))
        {
            Console.WriteLine($"Requested new folder at {_directory.FullName}");
        }
        
        ImGui.EndGroup();
        ImGui.Unindent();
    }
}