using ImGuiNET;
using System.Numerics;
using t3.graph;

namespace T3.Gui
{
    /// <summary>
    /// capsule T3 UI functionality from imgui-clutter in Program.cs
    /// </summary>
    public class T3UI
    {
        private static bool _view2Opened = true;

        public unsafe void DrawUI()
        {
            TableView.DrawTableView(ref _tableViewOpened);
            canvas1.Draw(ref _graphUIOpened, _mockModel.MainOp, "View1");
            canvas2.Draw(ref _view2Opened, _mockModel.MainOp, "View2");

            if (_showDemoWindow)
            {
                ImGui.ShowDemoWindow(ref _showDemoWindow);
            }
        }

        private static bool _tableViewOpened = true;
        private static bool _graphUIOpened = true;
        private static bool _showDemoWindow = true;
        private static GraphCanvas canvas1 = new GraphCanvas();
        private static GraphCanvas canvas2 = new GraphCanvas();


        public unsafe void InitStyle()
        {
            var style = ImGui.GetStyle();
            style.WindowRounding = 0;

            style.FramePadding = new Vector2(7, 4);
            style.ItemSpacing = new Vector2(4, 3);
            style.ItemInnerSpacing = new Vector2(3, 2);
            style.GrabMinSize = 2;
            style.FrameBorderSize = 0;
            style.WindowRounding = 3;
            style.ChildRounding = 1;
            style.ScrollbarRounding = 3;

            style.WindowRounding = 5.3f;
            style.FrameRounding = 2.3f;
            style.ScrollbarRounding = 0;

            style.Colors[(int)ImGuiCol.Text] = new Vector4(1, 1, 1, 0.85f);
            style.Colors[(int)ImGuiCol.Border] = new Vector4(0, 0.00f, 0.00f, 0.97f);
            style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 1.00f);
            style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.13f, 0.13f, 0.13f, 0.80f);
            style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.38f, 0.38f, 0.38f, 0.40f);
            style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.00f, 0.55f, 0.8f, 1.00f);
            style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.12f, 0.12f, 0.12f, 0.53f);
            style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.31f, 0.33f);
            style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.00f, 0.00f, 0.00f, 0.25f);
        }

        private static MockModel _mockModel = new MockModel();

    }
}
