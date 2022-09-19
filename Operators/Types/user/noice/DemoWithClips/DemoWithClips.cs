using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_827709f4_5e8e_46be_85c7_55ddd9878660
{
    public class DemoWithClips : Instance<DemoWithClips>
    {
        [Output(Guid = "150aa4ce-2971-4d14-8ce2-d6af90beb499")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

