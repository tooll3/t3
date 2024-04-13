using System.Numerics;
using ImGuiNET;

namespace SilkWindows.Implementations.FileManager.ItemDrawers;

internal sealed class DirectoryDrawer : FileSystemDrawer
{
    internal override string DisplayName => _displayDisplayName;
    internal override string Path => _directory.FullName;
    internal override bool IsDirectory => true;
    public DirectoryInfo DirectoryInfo => _directory;
    
    private readonly List<DirectoryDrawer> _directories = new();
    
    private readonly List<FileDrawer> _files = new();
    
    private bool _needsRescan;
    private readonly DirectoryInfo _directory;
    public override bool IsReadOnly { get; }
    private readonly string _displayDisplayName;
    private readonly string _expandedButtonLabel;
    private readonly string _collapsedButtonLabel;
    private readonly string _newSubfolderLabel;
    private readonly string _relativeDirectory;
    private readonly bool _isRoot;
    private readonly Action _beginChildren;
    private readonly Action _endChildren;
    
    public DirectoryDrawer(IFileManager fileManager, DirectoryInfo directory, bool isReadOnly, DirectoryDrawer? parent, string? alias = null) :
        base(fileManager, parent)
    {
        _directory = directory;
        _needsRescan = true;
        IsReadOnly = isReadOnly;
        _displayDisplayName = string.IsNullOrEmpty(alias) ? _directory.Name : '/' + alias;
        var buttonIdSuffix = "##" + _displayDisplayName;
        _expandedButtonLabel = IFileManager.ExpandedButtonLabel + buttonIdSuffix;
        _collapsedButtonLabel = IFileManager.CollapsedButtonLabel + buttonIdSuffix;
        _newSubfolderLabel = "*New subfolder" + buttonIdSuffix;
        _isRoot = parent == null;
        
        _beginChildren = _isRoot ? ImGui.Separator : ImGui.Indent;
        _endChildren = _isRoot ? () => { } : ImGui.Unindent;
        
        Expanded = _isRoot;
        
        var topParent = parent;
        
        while (topParent is { _isRoot: false })
        {
            topParent = topParent.ParentDirectoryDrawer;
        }
        
        if (topParent != null)
        {
            var relativePath = System.IO.Path.GetRelativePath(topParent.RootDirectory.FullName, _directory.FullName);
            var relativeDirectory = System.IO.Path.Combine(topParent._displayDisplayName, relativePath);
            _relativeDirectory = fileManager.FormatPathForDisplay(relativeDirectory);
        }
        else
        {
            _relativeDirectory = _displayDisplayName;
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
        if (!_isRoot)
        {
            var expandCollapseLabel = Expanded ? _expandedButtonLabel : _collapsedButtonLabel;
            
            ImGui.PushFont(fonts.Small);
            if (ImGui.Button(expandCollapseLabel))
            {
                Expanded = !Expanded;
            }
            
            ImGui.PopFont();
            ImGui.SameLine();
        }
        else
        {
            ImGui.PushFont(fonts.Large);
        }
        
        // we need extra padding between the rows so theres no blank space between them
        // the intuitive thing would be to allow files to be dropped in between directories like many other file managers do, but this is much easier
        // for the time being.
        var style = ImGui.GetStyle();
        var currentPadding = style.TouchExtraPadding;
        style.TouchExtraPadding = currentPadding with {Y = style.ItemSpacing.Y + style.FramePadding.Y};
        ImGui.Selectable(_displayDisplayName, isSelected);
        style.TouchExtraPadding = currentPadding;
        
        if (_isRoot)
        {
            ImGui.PopFont();
        }
    }
    
    protected override void DrawTooltip(ImFonts fonts)
    {
        ImGui.Text(_relativeDirectory);
        
        ImGui.PushFont(fonts.Small);
        ImGui.Text("Last modified: " + FileSystemInfo.LastWriteTime.ToShortDateString());
        ImGui.PopFont();
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
        
        _beginChildren();
        
        ImGui.BeginGroup();
        
        if (_directories.Count + _files.Count > 0)
        {
            foreach (var dir in _directories)
            {
                dir.Draw(fonts);
            }
            
            foreach (var file in _files)
            {
                file.Draw(fonts);
            }
        }
        else
        {
            // draw empty directory
            ImGui.TextDisabled("Empty directory");
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
        
        _endChildren();
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