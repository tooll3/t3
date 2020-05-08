using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui.UiHelpers;
using T3.Operators.Types.Id_8211249d_7a26_4ad0_8d84_56da72a5c536;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class GradientSliderUi
    {
        public static bool DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is GradientSlider gradientSlider))
                return false;

            var innerRect = selectableScreenRect;
            innerRect.Expand(-7);

            var gradient = gradientSlider.Gradient.Value;
            if (gradient == null)
            {
                //Log.Warning("Can't draw undefined gradient");
                return false;
            }
            
            // TODO: this is a work around for default initialization of reference data types
            if (gradientSlider.Gradient.Input.IsDefault)
            {
                gradientSlider.Gradient.TypedInputValue.Value = new Gradient(); 
            }            

            if (GradientEditor.Draw(gradient, drawList, innerRect))
            {
                gradientSlider.Gradient.DirtyFlag.Invalidate();
            }

            var x = gradientSlider.SamplePos.Value.Clamp(0,1)* innerRect.GetWidth() ;
            var pMin = new Vector2(innerRect.Min.X + x, innerRect.Min.Y);
            var pMax = new Vector2(innerRect.Min.X + x+2, innerRect.Max.Y);
            drawList.AddRectFilled(pMin, pMax, Color.Orange );
            
            return true;
        }
    }
}