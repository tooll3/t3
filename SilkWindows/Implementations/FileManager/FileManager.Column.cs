using ImGuiNET;
using SilkWindows.Implementations.FileManager.ItemDrawers;

namespace SilkWindows.Implementations.FileManager;

public sealed partial class FileManager
{
    private void DrawTable(ImFonts fonts, List<Column> expanded)
    {
        const ImGuiTableFlags tableFlags = ImGuiTableFlags.Resizable;
        const ImGuiTableColumnFlags columnFlags = ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.NoHide |
                                                  ImGuiTableColumnFlags.NoSort;
        
        if (ImGui.BeginTable(_tableId, expanded.Count, tableFlags))
        {
            ImGui.TableNextRow();
            for (var index = 0; index < expanded.Count; index++)
            {
                var column = expanded[index];
                var directoryDrawer = column.Drawer;
                
                ImGui.TableSetupColumn(column.Id, columnFlags);
                ImGui.TableSetColumnIndex(index);
                
                ImGui.BeginChild(directoryDrawer.Path);
                
                directoryDrawer.Draw(fonts);
                
                ImGui.EndChild();
            }
            
            ImGui.EndTable();
        }
    }
    
    private sealed class Column
    {
        public readonly string Id;
        public readonly DirectoryDrawer Drawer;
        public bool Drawn;
        
        public Column(DirectoryDrawer drawer, bool drawn, string id)
        {
            Id = id;
            Drawer = drawer;
            Drawn = drawn;
            drawer.ToggleButtonPressed = () => Drawn = !Drawn;
        }
    }
}