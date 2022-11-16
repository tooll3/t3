using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Operators.Types.Id_d6384148_c654_48ce_9cf4_9adccf91283a;

namespace T3.Editor.Gui.ChildUi
{
    public static class ValueSliderUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is ValueSlider valueSlider))
                return SymbolChildUi.CustomUiResult.None;

            var innerRect = selectableScreenRect;
            innerRect.Expand(-4);
            drawList.AddRect(innerRect.Min,innerRect.Max, Color.Black);
            

            var t = valueSlider.Input.Value.Clamp(0, 1);
            var x = innerRect.Min.X - _handleWSize.X/2f + innerRect.GetWidth() * t;
            drawList.AddRectFilled(innerRect.Min, new Vector2(x, innerRect.Max.Y), 
                                   new Color(0.3f,0.4f,1f,0.4f));
            
            ImGui.SetCursorScreenPos( new Vector2(x , innerRect.Min.Y) );
            ImGui.Button(" ", new Vector2(_handleWSize.X, innerRect.GetHeight()));

            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var newT = ((ImGui.GetMousePos().X - innerRect.Min.X)/ innerRect.GetWidth()).Clamp(0,1);
                
                valueSlider.Input.TypedInputValue.Value = newT;
                valueSlider.Input.Value = newT;
                valueSlider.Input.DirtyFlag.Invalidate();
            }
            
            ImGui.SameLine();
            ImGui.TextUnformatted($"  {valueSlider.Result.Value:0.00}");
            
            return SymbolChildUi.CustomUiResult.Rendered | SymbolChildUi.CustomUiResult.PreventInputLabels;
        }
        private static Vector2 _handleWSize = new Vector2(10,20);
    }
}