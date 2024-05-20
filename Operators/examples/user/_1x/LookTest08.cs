using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("5b81c4e6-1320-4d08-987f-a1d83ff62f4d")]
    public class LookTest08 : Instance<LookTest08>
    {

        [Output(Guid = "029c375a-44e1-4c12-a434-86eaa28199e4")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

