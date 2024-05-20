using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib._3d.rendering
{
    [Guid("5317ade3-d4df-480d-872d-a17c63909da0")]
    public class CameraExample : Instance<CameraExample>
    {
        [Output(Guid = "0c2f45b5-7591-45a6-9861-ce2575444dd4")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

