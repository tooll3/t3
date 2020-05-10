using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Operator;
using T3.Gui.ChildUi.Animators;
using T3.Gui.Styling;
using T3.Operators.Types.Id_11882635_4757_4cac_a024_70bb4e8b504c;
using T3.Operators.Types.Id_23794a1f_372d_484b_ac31_9470d0e77819;
using UiHelpers;
namespace T3.Gui.ChildUi
{
    public static class CounterUi
    {
        public static bool DrawChildUi(Instance instance, ImDrawListPtr drawList, ImRect screenRect)
        {
            if (!(instance is Counter counter))
                return false;
            
            if (AnimatorLabel.Draw(ref counter.Rate.TypedInputValue.Value, 
                                   screenRect, drawList, nameof(counter)))
            {
                counter.Rate.DirtyFlag.Invalidate();
            }

            if (MicroGraph.Draw(ref counter.Increment.TypedInputValue.Value, 
                                ref counter.Blending.TypedInputValue.Value, 
                                counter.Fragment, 
                                screenRect, drawList))
            {
                counter.Blending.DirtyFlag.Invalidate();
                counter.Increment.DirtyFlag.Invalidate();
            }
            return true;
        }
    }
}