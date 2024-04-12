using ImGuiNET;

namespace SilkWindows.Implementations.FileManager;

// todo: child class (directory) being referenced in the base class is UGLY
public abstract class FileSystemDrawer
{
    private protected readonly IFileManager FileManager;
    
    private protected FileSystemDrawer(IFileManager fileManager, DirectoryDrawer? parent)
    {
        FileManager = fileManager;
        ParentDirectoryDrawer = parent;
    }
    
    internal DirectoryInfo RootDirectory => ParentDirectoryDrawer == null ? new(FileSystemInfo.FullName) : ParentDirectoryDrawer.RootDirectory;
    public abstract bool IsReadOnly { get; }
    internal abstract bool IsDirectory { get; }
    internal abstract string Name { get; }
    internal abstract string Path { get; }
    
    internal DirectoryDrawer? ParentDirectoryDrawer { get; }
    
    protected abstract void DrawTooltip(ImFonts fonts);
    
    protected abstract FileSystemInfo FileSystemInfo { get; }
    
    protected virtual ImGuiHoveredFlags HoverFlags => ImGuiHoveredFlags.None;
    
    protected bool IsHovered() => ImGui.IsItemHovered(HoverFlags);
    
    protected bool HoveredByFileDrag(ImGuiHoveredFlags flags = ImGuiHoveredFlags.AllowWhenOverlappedByItem) =>
        FileManager.IsDraggingFiles && ImGui.IsItemHovered(flags);
    
    protected abstract void DrawSelectable(ImFonts fonts, bool isSelected);
    protected abstract void CompleteDraw(ImFonts fonts, bool hovered, bool isSelected);
    
    public void Draw(ImFonts fonts)
    {
        bool isSelected = FileManager.IsSelected(this);
        DrawSelectable(fonts, isSelected);
        var hovered = IsHovered();
        var hoveredByFileDrag = HoveredByFileDrag();
        
        if (hovered)
        {
            ImGui.SameLine();
            
            ImGui.PushFont(fonts.Small);
            ImGui.Text('\t' + FileSystemInfo.LastWriteTime.ToShortDateString());
            ImGui.PopFont();
            
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                FileManager.ItemClicked(this);
            }
            
            if (FileManager.HasDroppedFiles)
            {
                FileManager.ConsumeDroppedFiles(this);
            }
            
            if (ImGui.IsMouseDoubleClicked(0))
            {
                OnDoubleClicked();
            }
        }
        
        var isDropTarget = hoveredByFileDrag && FileManager.IsValidFileDropTarget(this);
        
        if (isDropTarget)
        {
            ImGui.Indent();
            ImGui.SeparatorText("Drop files here");
            ImGui.Unindent();
        }
        else
        {
            if (ImGui.BeginPopupContextItem())
            {
                DrawContextMenu(fonts);
            }
            
            if (!FileManager.IsDraggingFiles && hovered)
            {
                ImGui.BeginTooltip();
                DrawTooltip(fonts);
                ImGui.EndTooltip();
            }
        }
        
        CompleteDraw(fonts, hovered, isSelected);
    }
    
    protected abstract void DrawContextMenu(ImFonts fonts);
    
    protected abstract void OnDoubleClicked();
}