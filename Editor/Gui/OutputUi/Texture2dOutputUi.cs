using System.Diagnostics;
using Editor.Gui.Windows;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Editor.Gui.Windows;

namespace T3.Editor.Gui.OutputUi
{
    public class Texture2dOutputUi : OutputUi<Texture2D>
    {
        public override IOutputUi Clone()
        {
            return new Texture2dOutputUi()
                       {
                           OutputDefinition = OutputDefinition,
                           PosOnCanvas = PosOnCanvas,
                           Size = Size
                       };
        }

        protected override void DrawTypedValue(ISlot slot)
        {
            if (slot is Slot<Texture2D> typedSlot)
            {
                Texture2D texture = typedSlot.Value;
                ImageOutputCanvas.Current.DrawTexture(texture);
                ResourceManager.Instance().SecondRenderWindowTexture = texture;
            }
            else
            {
                Debug.Assert(false);
            }
        }
    }
}