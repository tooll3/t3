using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using T3.Core.Operator;

namespace T3.Gui.OutputUi
{
    public class ShaderResourceViewOutputUi : OutputUi<ShaderResourceView>
    {
        public override void DrawValue(ISlot slot)
        {
            if (slot is Slot<ShaderResourceView> typedSlot)
            {
                Invalidate(slot);
                _evaluationContext.Reset();
                var value = typedSlot.GetValue(_evaluationContext);
                if (value?.Description.Dimension == ShaderResourceViewDimension.Texture2D)
                {
                    ImGui.Image((IntPtr)value, new Vector2(100.0f, 100.0f));
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}