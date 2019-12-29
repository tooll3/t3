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
        public override IOutputUi Clone()
        {
            return new ShaderResourceViewOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       IsSelected = IsSelected,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        }
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<ShaderResourceView> typedSlot)
            {
                var value = typedSlot.Value;
                if (value?.Description.Dimension == ShaderResourceViewDimension.Texture2D)
                {
                    //TODO: This causes exception when rendered in output window
                    //ImGui.Image((IntPtr)value, new Vector2(100.0f, 100.0f));
                }
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}