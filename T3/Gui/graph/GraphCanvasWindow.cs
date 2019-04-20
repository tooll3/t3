using ImGuiNET;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A mock implementation of a future graph renderer
    /// </summary>
    public class GraphCanvasWindow
    {
        public GraphCanvasWindow(Instance opInstance, string windowTitle = "Graph windows")
        {
            _compositionOp = opInstance;
            _windowTitle = windowTitle;
            _canvas = new GraphCanvas(opInstance);
        }


        public bool Draw()
        {
            bool opened = true;

            if (ImGui.Begin(_windowTitle, ref opened))
            {
                _canvas.Draw();
            }
            ImGui.End();
            return opened;
        }

        private string _windowTitle;
        private GraphCanvas _canvas = null;
        private Instance _compositionOp;
    }
}