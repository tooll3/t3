using ImGuiNET;
using SharpDX.Direct3D11;
using System;
using System.Diagnostics;
using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Gui.Windows;

namespace T3.Gui.OutputUi
{
    public class Texture2dOutputUi : OutputUi<Texture2D>
    {
        public override void DrawValue(ISlot slot, bool recompute = true)
        {
            if (slot is Slot<Texture2D> typedSlot)
            {
                if (recompute)
                {
                    StartInvalidation(slot);
                    _evaluationContext.Reset();
                }
                var texture = recompute
                                ? typedSlot.GetValue(_evaluationContext) 
                                : typedSlot.Value;

                ImageOutputCanvas.Current.DrawTexture(texture);
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}