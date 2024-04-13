using ImGuiNET;

namespace SilkWindows.Implementations.FileManager.ItemDrawers;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
internal class FileDrawer : FileSystemDrawer
{
    internal sealed override string DisplayName => _file.Name;
    internal sealed override string Path => _file.FullName;
    internal sealed override bool IsDirectory => false;
    public sealed override bool IsReadOnly => ParentDirectoryDrawer!.IsReadOnly;
    
    private readonly FileInfo _file;
    private readonly string _relativePath;
    private readonly string _extensionDisplay;
    private readonly string _lastModifiedDisplay;
    
    public FileDrawer(IFileManager fileManager, FileInfo file, DirectoryDrawer parent) : base(fileManager, parent)
    {
        _file = file;
        _extensionDisplay = '[' + file.Extension + ']';
        
        if (ParentDirectoryDrawer == null)
            throw new InvalidOperationException("File drawer must have a parent directory drawer");
        
        var topParent = ParentDirectoryDrawer;
        while (topParent.ParentDirectoryDrawer != null)
            topParent = topParent.ParentDirectoryDrawer;
        
        var topParentDisplayName = topParent.DisplayName;
        var topParentPath = topParent.Path;
        var relativePath = System.IO.Path.GetRelativePath(topParentPath, Path);
        relativePath = System.IO.Path.Combine(topParentDisplayName, relativePath);
        _relativePath = fileManager.FormatPathForDisplay(relativePath);
        
        _lastModifiedDisplay = file.LastWriteTime.ToShortDateString();
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
        ImGui.Selectable(_file.Name, isCurrentlySelected);
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
        ImGui.Text(_relativePath);
    }
    
    protected sealed override FileSystemInfo FileSystemInfo => _file;
    
    public void DrawFileExtension(ImFonts fonts)
    {
        ImGui.PushFont(fonts.Small);
        ImGui.Text(_extensionDisplay);
        if (ImGui.BeginItemTooltip())
        {
            ImGui.Text("File Extension: " + _extensionDisplay);
            ImGui.EndTooltip();
        }
        ImGui.PopFont();
    }
    
    public void DrawSize(ImFonts fonts)
    {
        const double kb = 1024;
        const double mb = kb * 1024;
        const double toKb = 1 / kb;
        const double toMb = 1 / mb;
        var sizeInBytes = _file.Length;
        
        var showInMb = sizeInBytes > mb;
        var display = showInMb ? $"{sizeInBytes * toMb: 0.0} MB" : $"{sizeInBytes * toKb: 0.0} KB";
        ImGui.PushFont(fonts.Small);
        ImGui.Text(display);
        if (ImGui.BeginItemTooltip())
        {
            ImGui.Text("Size: " + display);
            ImGui.EndTooltip();
        }
        ImGui.PopFont();
    }
    
    public void DrawLastModified(ImFonts fonts)
    {
        ImGui.PushFont(fonts.Small);
        ImGui.Text(_lastModifiedDisplay);
        
        if (ImGui.BeginItemTooltip())
        {
            ImGui.Text("Time last modified: " + _lastModifiedDisplay);
            ImGui.EndTooltip();
        }
        ImGui.PopFont();
    }
}