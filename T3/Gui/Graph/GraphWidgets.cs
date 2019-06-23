using ImGuiNET;
using System.Numerics;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Renders a small representation of the operator's parameters and other data
    /// </summary>
    public class NodeDetailsPanel
    {
        static public void Draw(SymbolChildUi ui)
        {
            ImGui.SetCursorPos(GraphCanvas.Current.ChildPosFromCanvas(ui.PosOnCanvas + ui.Size));

            ImGui.BeginChildFrame((uint)ui.SymbolChild.Id.GetHashCode(), new Vector2(230, 100));
            {
                //for (int i = 0; i < 10; i++)
                //{

                //    ImGui.Text("hallo" + i);
                //}
            }
            ImGui.EndChildFrame();
        }
    }
}
