using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.Styling;
using T3.Operators.Types.Id_000e08d0_669f_48df_9083_7aa0a43bbc05;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class GpuMeasureUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is GpuMeasure measaureInstance))
                return SymbolChildUi.CustomUiResult.None;

            var symbolChild = measaureInstance.Parent.Symbol.Children.Single(c => c.Id == measaureInstance.SymbolChildId);
            ImGui.PushClipRect(selectableScreenRect.Min, selectableScreenRect.Max, true);
            
            float h = selectableScreenRect.GetHeight();
            var font = h > 50 ? Fonts.FontLarge : h > 25 ? Fonts.FontNormal : Fonts.FontSmall;

            ImGui.PushFont(font);
            ImGui.SetCursorScreenPos(selectableScreenRect.Min + new Vector2(10,0));
            ImGui.BeginGroup();
            if (!string.IsNullOrEmpty(symbolChild.Name))
            {
                ImGui.Text(symbolChild.Name);
            }

            ImGui.Text($"{measaureInstance.LastMeasureInMicroSeconds}us");
            ImGui.EndGroup();
            ImGui.PopFont();
            
            ImGui.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered;
        }
    }
}