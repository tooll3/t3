using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5ea8bc54_d1f6_4748_9839_e3e4415a5608
{
    public class ThereDemo : Instance<ThereDemo>
    {
        [Output(Guid = "9316bc94-c0d3-45a4-9fab-ae9608510b04")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

