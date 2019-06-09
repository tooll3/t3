using ImGuiNET;

namespace T3.Gui.Animation.Curve
{
    /// <summary>
    /// A stub window to collect curve editing functionality during implementation.
    /// ToDo:
    /// [ ] Generate a mock curve with some random keyframes
    /// [ ] Render time-line ticks
    /// [ ] Zoom and pan timeline-range
    /// [ ] Render value area
    /// [ ] Mock random-keyframes
    /// [ ] Render Curve
    /// [ ] Selection of keyframes
    /// [ ] Edit Keyframes-tangent editing
    /// [ ] Implement Curve-Edit-Box
    /// </summary>
    class CurveEditorWindow
    {
        public CurveEditorWindow()
        {

        }


        public bool Draw(ref bool opened)
        {

            if (ImGui.Begin("Curve Editor", ref opened))
            {
                ImGui.Text("Hello!");
                //Canvas.Draw();
            }

            ImGui.End();
            return opened;
        }
    }


}
