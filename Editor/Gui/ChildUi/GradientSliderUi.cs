using System.Numerics;
using ImGuiNET;
using T3.Core.Operator;
using T3.Core.Utils;
using T3.Editor.Gui.ChildUi.WidgetUi;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;
using T3.Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536;

namespace T3.Editor.Gui.ChildUi
{
    public static class GradientSliderUi
    {
        public static SymbolChildUi.CustomUiResult DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (instance is not SampleGradient gradientInstance
                || !ImGui.IsRectVisible(selectableScreenRect.Min, selectableScreenRect.Max))
                return SymbolChildUi.CustomUiResult.None;

            var dragWidth = WidgetElements.DrawDragIndicator(selectableScreenRect, drawList);
            var innerRect = selectableScreenRect;
            innerRect.Min.X += dragWidth;
            
            var gradient = (gradientInstance.Gradient.HasInputConnections) 
                               ? gradientInstance.Gradient.Value 
                               :gradientInstance.Gradient.TypedInputValue.Value;
            
            if (gradient != null)
            {
                //Log.Warning("Can't draw undefined gradient");
                var cloneIfModified = gradientInstance.Gradient.Input.IsDefault;
                
                if (GradientEditor.Draw(ref gradient, drawList, innerRect, cloneIfModified))
                {
                    if (cloneIfModified)
                    {
                        gradientInstance.Gradient.SetTypedInputValue(gradient);
                    }
                    gradientInstance.Color.DirtyFlag.Invalidate();
                    gradientInstance.OutGradient.DirtyFlag.Invalidate();
                }

                var x = gradientInstance.SamplePos.Value.Clamp(0, 1) * innerRect.GetWidth();
                var pMin = new Vector2(innerRect.Min.X + x, innerRect.Min.Y);
                var pMax = new Vector2(innerRect.Min.X + x + 2, innerRect.Max.Y);
                drawList.AddRectFilled(pMin, pMax, UiColors.StatusAnimated);
                //return SymbolChildUi.CustomUiResult.None;
            }


            return SymbolChildUi.CustomUiResult.Rendered 
                   | SymbolChildUi.CustomUiResult.PreventInputLabels 
                   | SymbolChildUi.CustomUiResult.PreventOpenSubGraph 
                   | SymbolChildUi.CustomUiResult.PreventTooltip 
                   | SymbolChildUi.CustomUiResult.PreventOpenParameterPopUp;
        }
    }
}