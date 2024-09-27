using ImGuiNET;

namespace T3.Editor.Gui.Graph;

/// <summary>
/// Provides a list of operators
/// </summary>
class CreateOperatorWindow
{
    public void Draw()
    {
        if (ImGui.Begin("Select operator", ref _opened))
        {
            ImGui.Button("Yeah");
        }
        ImGui.End();
    }

    private static bool _opened = true;
}