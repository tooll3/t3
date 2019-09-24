using System;
using System.Numerics;
using ImGuiNET;

namespace T3.graph
{
    public static class TableView
    {
        public static void DrawTableView(ref bool opened)
        {
            // ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 2)); // avoid gaps between colums

            if (!ImGui.Begin("Table View")) { ImGui.End(); return; }
            {
                ImGui.DragInt("Columns", ref _columnCount, v_speed: 1, v_min: 1, v_max: 20);
                ImGui.Columns(_columnCount, "TableView", false);

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 1));
                random = new Random(0);
                for (int colIndex = 0; colIndex < _columnCount; colIndex++)
                {
                    for (int rowIndex = 0; rowIndex < 50; rowIndex++)
                    {
                        var f = (float)random.NextDouble();
                        ImGui.PushItemWidth(-1);
                        ImGui.DragFloat($"##{colIndex}:{rowIndex}", ref f);
                        ImGui.PopItemWidth();
                    }
                    ImGui.NextColumn();
                }
                ImGui.PopStyleVar();
            }
            ImGui.End();
            // ImGui.PopStyleVar();
        }

        private static int _columnCount = 3;
        private static Random random = new Random();
    }
}