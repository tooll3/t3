using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b849e7a2_2fee_4463_b964_67f5c2519fc5
{
    public class SceneCityLogo : Instance<SceneCityLogo>
    {
        [Output(Guid = "0691231b-64cc-40a4-aa54-d31285d0928b")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

