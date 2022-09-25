using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_82a1df85_9788_4d39_8e23_f42633cd108d
{
    public class PointExamples : Instance<PointExamples>
    {
        [Output(Guid = "8e807be5-6c36-47ee-afb9-d5712dd83406")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

