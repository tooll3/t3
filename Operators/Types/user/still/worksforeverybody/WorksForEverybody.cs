using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_00485ce8_d342_4c97_aac3_1af8a7f03897
{
    public class WorksForEverybody : Instance<WorksForEverybody>
    {
        [Output(Guid = "de57341f-86c1-4426-b3c6-a5dc36490759")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

