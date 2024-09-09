using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_000e08d0_669f_48df_9083_7aa0a43bbc05;

namespace T3.Editor.Gui.ChildUi
{
    public static class GpuMeasureUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is GpuMeasure measureInstance))
                return SymbolChildUi.CustomUiResult.None;

            var symbolChild = measureInstance.Parent.Symbol.Children.Single(c => c.Id == measureInstance.SymbolChildId);
            ImGui.PushClipRect(selectableScreenRect.Min, selectableScreenRect.Max, true);
            
            float h = selectableScreenRect.GetHeight();
            var font = h > 50 ? Fonts.FontLarge : h > 25 ? Fonts.FontNormal : Fonts.FontSmall;

            var radius = measureInstance.LastMeasureInMs * 5;
            if (radius > 2)
            {
                drawList.AddCircleFilled(selectableScreenRect.GetCenter(), radius, _color);
            }

            ImGui.PushFont(font);
            ImGui.SetCursorScreenPos(selectableScreenRect.Min + new Vector2(10,0));
            ImGui.BeginGroup();
            if (!string.IsNullOrEmpty(symbolChild.Name))
            {
                ImGui.TextUnformatted(symbolChild.Name);
            }

            ImGui.TextUnformatted($"{measureInstance.LastMeasureInMicroSeconds}µs");
            ImGui.EndGroup();
            ImGui.PopFont();
            
            ImGui.PopClipRect();
            return SymbolChildUi.CustomUiResult.Rendered;
        }

        private static Color _color = new(0.8f, 0.6f, 0.2f, 0.2f);
    }
}