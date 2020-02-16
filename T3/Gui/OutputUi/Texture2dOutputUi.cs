using ImGuiNET;
using SharpDX.Direct3D11;
using System;
using System.Diagnostics;
using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Gui.Windows;

namespace T3.Gui.OutputUi
{
    public class Texture2dOutputUi : OutputUi<Texture2D>
    {
        public override IOutputUi Clone()
        {
            return new Texture2dOutputUi()
                   {
                       OutputDefinition = OutputDefinition,
                       IsSelected = IsSelected,
                       PosOnCanvas = PosOnCanvas,
                       Size = Size
                   };
        }
        
        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<Texture2D> typedSlot)
            {
                ImageOutputCanvas.Current.DrawTexture(typedSlot.Value);
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}