using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dad16e56_8fed_4013_b7ee_64825bb058d4
{
    public class CurveTest : Instance<CurveTest>
    {
        [Output(Guid = "4177242b-b316-4f24-bde8-23961be0e07f")]
        public readonly Slot<Texture2D> Texture = new Slot<Texture2D>();

        [Input(Guid = "d34e16a9-4346-4dcf-a1fb-11325925dcd7")]
        public readonly InputSlot<T3.Core.Animation.Curve> CurveA = new InputSlot<T3.Core.Animation.Curve>();

        [Input(Guid = "d2824633-c023-4e26-a61b-6f15718a119e")]
        public readonly InputSlot<T3.Core.Animation.Curve> CurveB = new InputSlot<T3.Core.Animation.Curve>();


    }
}

