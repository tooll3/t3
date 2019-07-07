using ImGuiNET;
using imHelpers;
using System.Collections.Generic;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.Animation;

namespace T3.Gui.Graph
{
    /// <summary>
    /// A window that renders a node graph 
    /// </summary>
    public class GraphCanvasWindow
    {
        public GraphCanvasWindow(Instance opInstance, string windowTitle = "Graph windows")
        {
            _windowTitle = windowTitle;
            Canvas = new GraphCanvas(opInstance);
        }

        private float GetGraphHeight()
        {
            return ImGui.GetWindowHeight() - _heightTimeLine - 30;
        }


        public bool Draw()
        {
            bool opened = true;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
            if (ImGui.Begin(_windowTitle, ref opened))
            {
                SplitFromBottom(ref _heightTimeLine);
                ImGui.BeginChild("##graph", new Vector2(0, GetGraphHeight()), false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
                {
                    var dl = ImGui.GetWindowDrawList();
                    dl.ChannelsSplit(2);
                    dl.ChannelsSetCurrent(1);
                    {
                        ImGui.SetCursorScreenPos(ImGui.GetWindowPos() + new Vector2(1, 1));
                        DrawBreadcrumbs();
                    }
                    dl.ChannelsSetCurrent(0);
                    Canvas.Draw();
                    dl.ChannelsMerge();
                }
                ImGui.EndChild();
                ImGui.BeginChild("##timeline", Vector2.Zero, false, ImGuiWindowFlags.NoMove);
                {
                    DrawTimeline();
                }
                ImGui.EndChild();


            }
            ImGui.PopStyleVar();

            ImGui.End();
            return opened;
        }



        private void DrawTimeline()
        {
            var opened = true;
            curveEditor.Draw(ref opened);
        }

        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>


        private static float _heightTimeLine = 100;
        private CurveEditor curveEditor = new CurveEditor();

        /// <summary>Draw a splitter</summary>
        /// <remarks>
        /// Take from https://github.com/ocornut/imgui/issues/319#issuecomment-147364392
        /// </remarks>
        void SplitFromBottom(ref float offsetFromBottom)
        {
            const float thickness = 5; ;

            var backup_pos = ImGui.GetCursorPos();
            ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - offsetFromBottom - thickness);

            ImGui.GetWindowContentRegionMax();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(1, 0, 0, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.6f, 0.6f, 0.6f, 0.10f));
            ImGui.Button("##Splitter", new Vector2(-1, thickness));

            ImGui.PopStyleColor(3);

            THelpers.DebugWindowRect("Child");
            //ImGui.SetItemAllowOverlap(); // Allow having other buttons OVER our splitter. 

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNS);
            }

            if (ImGui.IsItemActive())
            {
                offsetFromBottom = Im.Clamp(
                    offsetFromBottom - ImGui.GetIO().MouseDelta.Y,
                    0,
                    ImGui.GetWindowContentRegionMax().Y - 30 - thickness);
            }

            ImGui.SetCursorPos(backup_pos);
        }



        private void DrawBreadcrumbs()
        {
            List<Instance> parents = Canvas.GetParents();

            foreach (var p in parents)
            {
                ImGui.PushID(p.Id.GetHashCode());
                if (ImGui.Button(p.Symbol.Name))
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
            ImGui.Button(Canvas.CompositionOp.Symbol.Name);
            ImGui.PopStyleColor(2);
        }



        private string _windowTitle;
        public GraphCanvas Canvas { get; private set; }
    }
}