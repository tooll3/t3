using System.Linq;
using System.Numerics;
using ImGuiNET;
using lib.dx11;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace libEditor.CustomUi;

public static class GpuMeasureUi
{
    public static SymbolUi.Child.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect, Vector2 canvasScale)
    {
        if (!(instance is GpuMeasure measureInstance))
            return SymbolUi.Child.CustomUiResult.None;

        var symbolChild = instance.SymbolChild;
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
        if (!string.IsNullOrWhiteSpace(symbolChild.Name))
        {
            ImGui.TextUnformatted(symbolChild.Name);
        }

        ImGui.TextUnformatted($"{measureInstance.LastMeasureInMicroSeconds}µs");
        ImGui.EndGroup();
        ImGui.PopFont();
            
        ImGui.PopClipRect();
        return SymbolUi.Child.CustomUiResult.Rendered;
    }

    private static Color _color = new(0.8f, 0.6f, 0.2f, 0.2f);
}