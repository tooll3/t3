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
            Canvas = new GraphCanvas(opInstance);
        }


        public bool Draw()
        {
            bool opened = true;

            if (ImGui.Begin(_windowTitle, ref opened))
            {
                DrawBreadcrumbs();
                Canvas.Draw();
            }

            ImGui.End();
            return opened;
        }

        private void DrawBreadcrumbs()
        {
            List<Instance> parents = Canvas.GetParents();

            foreach (var p in parents)
            {
                ImGui.PushID(p.Id.GetHashCode());
                if (ImGui.Button(p.Symbol.SymbolName))
                {
                    Canvas.CompositionOp = p;
                }

                ImGui.SameLine();
                ImGui.PopID();
                ImGui.Text("/");
                ImGui.SameLine();
            }

            ImGui.PushStyleColor(ImGuiCol.Button, Color.White.Rgba);
            ImGui.PushStyleColor(ImGuiCol.Text, Color.Black.Rgba);
            ImGui.Button(Canvas.CompositionOp.Symbol.SymbolName);
            ImGui.PopStyleColor(2);
        }



        private string _windowTitle;
        public GraphCanvas Canvas { get; private set; }
    }
}