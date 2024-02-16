using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.img.use
{
	[Guid("1fdd634f-4c6a-4615-b75a-0c46732c9826")]
    public class UseRenderTargetExample : Instance<UseRenderTargetExample>
    {
        [Output(Guid = "2f32cf47-be6e-4ac8-a2e5-6e967edb64b1")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

