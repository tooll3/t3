using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.InputUi;
using T3.Operators.Types.Id_5d7d61ae_0a41_4ffa_a51d_93bab665e7fe;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class ValueUi
    {
        public static bool DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is Value valueInstance))
                return false;

            // var valueSymbolUi = SymbolUiRegistry.Entries[instance.Symbol.Id];
            // var inputUi = (FloatInputUi)valueSymbolUi.InputUis[valueInstance.Float.Id];
            // ImGui.SetCursorScreenPos(selectableScreenRect.Min + Vector2.One * 10);
            // ImGui.SetNextWindowSize(new Vector2(selectableScreenRect.GetWidth(), selectableScreenRect.GetHeight()));
            // bool edited = ImGui.SliderFloat("value", ref valueInstance.Float.TypedInputValue.Value, inputUi.Min, inputUi.Max);
            // if (edited)
            // {
            //     // todo: trigger for all instances
            //     valueInstance.Float.DirtyFlag.Invalidate();
            // }

            var opacity = (float)Math.Sin(ImGui.GetTime());
            drawList.AddRectFilled(selectableScreenRect.Min, selectableScreenRect.Max, new Color(1, 1, 0, 0.2f * opacity));
            ImGui.SetCursorScreenPos(selectableScreenRect.Min + Vector2.One * 10);
            ImGui.Text($"{valueInstance.Result.Value:0.00}");

            return true;
        }
    }
}