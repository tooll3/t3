using ImGuiNET;

namespace SilkWindows.Implementations.FileManager;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
internal class FileDrawer : IFileSystemDrawer
{
    public string Name => _name;
    public string Path => _file.FullName;
    public bool Expanded { get; set; }
    public IFileSystemDrawer? ParentDirectoryDrawer => _parent;
    public bool IsDirectory => false;
    public FileSystemInfo FileSystemInfo => _file;
    
    private string _name;
    private string _extension;
    private string _displayText;
    private readonly FileInfo _file;
    private readonly DirectoryDrawer _parent;
    private readonly IFileManager _fileManager;
    private readonly Func<bool, bool> _drawBasic;
    private readonly Func<bool, bool> _drawExpanded;
    
    public FileDrawer(IFileManager fileManager, FileInfo file, DirectoryDrawer parent)
    {
        _name = file.Name;
        _extension = file.Extension;
        _file = file;
        _parent = parent;
        _fileManager = fileManager;
        _displayText = $"[{_extension}] {_name}";
        _drawBasic = DrawBasic;
        _drawExpanded = DrawExpanded;
    }
    
    public void Draw()
    {
        if (DrawBasic(_fileManager.IsSelected(this)))
        {
            _fileManager.ItemClicked(this);
        }
        
        var isSelected = _fileManager.IsSelected(this);
        var clicked = Expanded ? DrawExpanded(isSelected) : DrawBasic(isSelected);
        if (clicked)
        {
            _fileManager.ItemClicked(this);
        }
        
        if (ImGui.IsItemHovered())
        {
            if (_fileManager.HasDroppedFiles)
            {
                _parent.DropInFiles();
            }
            else if (ImGui.IsMouseDoubleClicked(0))
            {
                _fileManager.DoubleClicked(this, false);
            }
            else if (ImGui.BeginTooltip())
            {
                DrawTooltip();
                ImGui.EndTooltip();
            }
        }
    }
    
    /// <summary>
    /// Must return true if the item is clicked
    /// </summary>
    /// <param name="isCurrentlySelected"></param>
    /// <returns></returns>
    protected virtual bool DrawBasic(bool isCurrentlySelected)
    {
        return ImGui.Selectable(_displayText, isCurrentlySelected);
    }
    
    /// <summary>
    /// Must return true if the item is clicked
    /// </summary>
    /// <param name="isCurrentlySelected"></param>
    protected virtual bool DrawExpanded(bool isCurrentlySelected)
    {
        // does nothing extra right now - can be overridden
        return DrawBasic(isCurrentlySelected);
    }
    
    protected virtual void DrawTooltip()
    {
        const double kb = 1024;
        const double mb = kb * 1024;
        const double toKb = 1 / kb;
        const double toMb = 1 / mb;
        var sizeInBytes = _file.Length;
        
        var showInMb = sizeInBytes > mb;
        ImGui.Text("size: ");
        ImGui.SameLine();
        ImGui.Text(showInMb ? $"{sizeInBytes * toMb: 0.0} MB" : $"{sizeInBytes * toKb: 0.0} KB");
    }
}