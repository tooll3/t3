using System;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_660922c6_60d8_4d21_bec7_e6cdfb1675c0
{
    public class TestVj : Instance<TestVj>
    {
        [Output(Guid = "d1b92ff3-bdde-40da-ad15-c8c343685138")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

    }
}

