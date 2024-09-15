using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.img.fx
{
    [Guid("eccd22ed-1a59-4655-b811-10790871cd4c")]
    public class SharpenExample : Instance<SharpenExample>
    {
        [Output(Guid = "edc2dd9c-0a39-42a5-8af5-ea67e639d2f3")]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();


    }
}

