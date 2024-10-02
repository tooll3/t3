using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_442d40e3_7c00_4161_a606_79c2fe6c36c1
{
    public class CupidDemo : Instance<CupidDemo>
    {
        [Output(Guid = "021568ee-42fc-4367-b652-0adb5397642e")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

