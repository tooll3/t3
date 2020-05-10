using ImGuiNET;
using T3.Core.Operator;
using T3.Gui.ChildUi.Animators;
using T3.Operators.Types.Id_23794a1f_372d_484b_ac31_9470d0e77819;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class Jitter2dUi
    {
        public static bool DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect selectableScreenRect)
        {
            if (!(instance is Jitter2d jitter2d))
                return false;

            var innerRect = selectableScreenRect;
            innerRect.Expand(-4);
            var h = innerRect.GetWidth() * 0.2f;

            if (RateLabel.Draw(ref jitter2d.Rate.TypedInputValue.Value, 
                               innerRect, drawList, nameof(jitter2d)))
            {
                jitter2d.Rate.DirtyFlag.Invalidate();
            }

            if (MicroGraph.Draw(ref jitter2d.JumpDistance.TypedInputValue.Value, 
                                ref jitter2d.Blending.TypedInputValue.Value, 
                                jitter2d.Fragment, 
                                innerRect, drawList))
            {
                jitter2d.Blending.DirtyFlag.Invalidate();
                jitter2d.JumpDistance.DirtyFlag.Invalidate();
            }
            return true;
        }
    }
}