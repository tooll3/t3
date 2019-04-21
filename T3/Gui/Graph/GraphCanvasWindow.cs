using ImGuiNET;
using System.Collections.Generic;
using T3.Core.Operator;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphCanvasWindow
    {
        public GraphCanvasWindow(Instance opInstance, string windowTitle = "Graph windows")
        {
            //_compositionOp = opInstance;
            _windowTitle = windowTitle;
            _canvas = new GraphCanvas(opInstance);
        }


        public bool Draw()
        {
            bool opened = true;

            if (ImGui.Begin(_windowTitle, ref opened))
            {
                DrawBreadcrumbs();
                _canvas.Draw();
            }
            ImGui.End();
            return opened;
        }

        private void DrawBreadcrumbs()
        {
            var parents = new List<Instance>();
            var op = _canvas.CompositionOp;
            while (op.Parent != null)
            {
                op = op.Parent;
                parents.Insert(0, op);
            }

            foreach (var p in parents)
            {
                ImGui.PushID(p.Id.GetHashCode());
                if (ImGui.Button(p.Symbol.SymbolName))
                {
                    _canvas.CompositionOp = p;
                }
                ImGui.SameLine();
                ImGui.PopID();
                ImGui.Text("/");
                ImGui.SameLine();
            }
            ImGui.PushStyleColor(ImGuiCol.Button, Color.White.Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Black.Rgba);
            ImGui.Button(_canvas.CompositionOp.Symbol.SymbolName);
            ImGui.PopStyleColor(2);
        }


        private string _windowTitle;
        private GraphCanvas _canvas = null;
    }
}