using ImGuiNET;
using UiHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using T3.Core.Animation.Curve;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.Selection;
using static T3.Core.Animation.Curve.Utils;

namespace T3.Gui.Animation
{
    /// <summary>
    /// A stub window to collect curve editing functionality during implementation.
    /// [ ] Implement Curve-Edit-Box
    /// </summary>
    public class CurveEditorWindow
    {
        public bool Draw(ref bool opened)
        {

            if (ImGui.Begin("Curve Editor", ref opened))
            {
                WindowPos = ImGui.GetWindowPos();
                WindowSize = ImGui.GetWindowSize();

                ImGui.BeginGroup();
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(1, 1));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
                    ImGui.PushStyleColor(ImGuiCol.WindowBg, new Color(60, 60, 70, 200).Rgba);

                    _canvas.Draw();
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar(2);
                }
                ImGui.EndGroup();
            }
            ImGui.End();
            return opened;
        }


        public Vector2 WindowSize { get; private set; }

        /// <summary>
        /// Position of the canvas window-panel within Application window
        /// </summary>
        public Vector2 WindowPos { get; private set; }

        private CurveEditCanvas _canvas = new CurveEditCanvas();
    }
}
