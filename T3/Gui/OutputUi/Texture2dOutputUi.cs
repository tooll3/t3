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


        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<Texture2D> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var texture = typedSlot.GetValue(_evaluationContext);
                ImageOutputCanvas.Current.DrawTexture(texture);
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}