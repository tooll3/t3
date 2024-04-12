using ImGuiNET;

namespace SilkWindows.Implementations.FileManager;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
internal class FileDrawer : FileSystemDrawer
{
    internal sealed override string Name => _name;
    internal sealed override string Path => _file.FullName;
    protected bool Expanded;
    internal sealed override bool IsDirectory => false;
    public sealed override bool IsReadOnly => ParentDirectoryDrawer!.IsReadOnly;
    
    private readonly string _name;
    private readonly string _displayText;
    private readonly FileInfo _file;
    
    public FileDrawer(IFileManager fileManager, FileInfo file, DirectoryDrawer parent) : base(fileManager, parent)
    {
        _name = file.Name;
        _file = file;
        _displayText = $"[{file.Extension}] {_name}";
        
        if(ParentDirectoryDrawer == null)
            throw new InvalidOperationException("File drawer must have a parent directory drawer");
    }
    
    protected override void DrawSelectable(ImFonts fonts, bool isSelected)
    {
        if (Expanded)
            DrawExpanded(isSelected, fonts);
        else
            DrawBasic(isSelected, fonts);
    }
    
    protected override void CompleteDraw(ImFonts fonts, bool hovered, bool isSelected)
    {
    }
    
    protected sealed override void OnDoubleClicked() => FileManager.DoubleClicked(this, false);
    
    /// <summary>
    /// Top item in the stack must be the selectable/hoverable item
    /// </summary>
    protected virtual void DrawBasic(bool isCurrentlySelected, ImFonts fonts)
    {
        ImGui.Selectable(_displayText, isCurrentlySelected);
    }
    
    /// <summary>
    /// Top item in the stack must be the selectable/hoverable item
    /// </summary>
    protected virtual void DrawExpanded(bool isCurrentlySelected, ImFonts fonts)
    {
        // does nothing extra right now - can be overridden
        DrawBasic(isCurrentlySelected, fonts);
    }
    
    protected override void DrawContextMenu(ImFonts fonts)
    {
        if (ImGui.MenuItem("Open in system file manager"))
        {
            FileManager.ShowInSystemFileManager(this);
        }
        
        if (ImGui.MenuItem("Open in external editor"))
        {
            FileManager.DoubleClicked(this, true);
        }
    }
    
    protected override void DrawTooltip(ImFonts fonts)
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
    
    protected sealed override FileSystemInfo FileSystemInfo => _file;
}