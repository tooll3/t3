using ImGuiNET;

namespace SilkWindows.Implementations.FileManager;

internal sealed class DirectoryDrawer : FileSystemDrawer
{
    internal override string Name => _directory.Name;
    internal override string Path => _directory.FullName;
    internal override bool IsDirectory => true;
    public DirectoryInfo DirectoryInfo => _directory;
    
    private readonly List<DirectoryDrawer> _directories = new();
    
    private readonly List<FileDrawer> _files = new();
    
    protected bool Expanded { get; private set; }
    
    private bool _needsRescan;
    private readonly DirectoryInfo _directory;
    public override bool IsReadOnly { get; }
    private readonly string _displayName;
    private readonly string _expandedButtonLabel;
    private readonly string _collapsedButtonLabel;
    private readonly string _newSubfolderLabel;
    private readonly string _relativeDirectory;
    
    public DirectoryDrawer(IFileManager fileManager, DirectoryInfo directory, bool isReadOnly, DirectoryDrawer? parent, string? alias = null) :
        base(fileManager, parent)
    {
        _directory = directory;
        _needsRescan = true;
        IsReadOnly = isReadOnly;
        _displayName = string.IsNullOrEmpty(alias) ? _directory.Name : '/' + alias;
        var buttonIdSuffix = "##" + _displayName;
        _expandedButtonLabel = "[-]" + buttonIdSuffix;
        _collapsedButtonLabel = "[+]" + buttonIdSuffix;
        _newSubfolderLabel = "*New subfolder" + buttonIdSuffix;
        
        var topParent = parent;
        while (topParent?.ParentDirectoryDrawer != null)
        {
            topParent = topParent.ParentDirectoryDrawer;
        }
        
        if (topParent != null)
        {
            var relativePath = System.IO.Path.GetRelativePath(topParent.RootDirectory.FullName, _directory.FullName);
            _relativeDirectory = System.IO.Path.Combine(topParent._displayName, relativePath);
        }
        else
        {
            _relativeDirectory = _displayName;
        }
    }
    
    public void MarkNeedsRescan() => _needsRescan = true;
    
    private void Rescan()
    {
        ClearChildren();
        
        var contents = FileManager.GetDirectoryContents(_directory);
        foreach (var dir in contents)
        {
            switch (dir)
            {
                case DirectoryInfo di:
                    _directories.Add(new DirectoryDrawer(FileManager, di, IsReadOnly, this));
                    break;
                case FileInfo fi:
                    _files.Add(new FileDrawer(FileManager, fi, this));
                    break;
            }
        }
        
        return;
        
        void ClearChildren()
        {
            for (int i = _directories.Count - 1; i >= 0; i--)
            {
                FileManager.RemoveFromSelection(_directories[i]);
                _directories.RemoveAt(i);
            }
            
            for (int i = _files.Count - 1; i >= 0; i--)
            {
                FileManager.RemoveFromSelection(_files[i]);
                _files.RemoveAt(i);
            }
        }
    }
    
    protected override void DrawSelectable(ImFonts fonts, bool isSelected)
    {
        var expandCollapseLabel = Expanded ? _expandedButtonLabel : _collapsedButtonLabel;
        if (ImGui.Button(expandCollapseLabel))
        {
            Expanded = !Expanded;
        }
        
        ImGui.SameLine();
        
        ImGui.Selectable(_displayName, isSelected);
    }
    
    protected override void DrawTooltip(ImFonts fonts)
    {
        if (!IsReadOnly)
            ImGui.Text(_directory.FullName);
        else
            ImGui.Text(_relativeDirectory);
    }
    
    protected override FileSystemInfo FileSystemInfo => _directory;
    
    protected override void CompleteDraw(ImFonts fonts, bool hovered, bool isSelected)
    {
        if (!Expanded)
        {
            return;
        }
        
        if (_needsRescan)
        {
            Rescan();
            _needsRescan = false;
        }
        
        ImGui.Indent();
        ImGui.BeginGroup();
        
        foreach (var dir in _directories)
        {
            dir.Draw(fonts);
        }
        
        foreach (var file in _files)
        {
            file.Draw(fonts);
        }
        
        if (!IsReadOnly)
        {
            ImGui.PushFont(fonts.Small);
            if (ImGui.SmallButton(_newSubfolderLabel))
            {
                FileManager.Log(this, $"Requested new folder at {_directory.FullName}");
                FileManager.CreateNewSubfolder(this);
            }
            
            ImGui.PopFont();
            
            if (ImGui.IsItemHovered() && FileManager.HasDroppedFiles)
            {
                FileManager.CreateNewSubfolder(this, true);
            }
        }
        
        ImGui.EndGroup();
        ImGui.Unindent();
    }
    
    protected override void DrawContextMenu(ImFonts fonts)
    {
        if (ImGui.MenuItem("Refresh"))
        {
            MarkNeedsRescan();
        }
    }
    
    protected override void OnDoubleClicked() => Expanded = !Expanded;
}