using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ae52baa3_9bd8_4e35_95c7_4811a55eaf7d
{
    public class BarnVJExperiment : Instance<BarnVJExperiment>
    {
        [Output(Guid = "fa5efe86-4faa-463a-a4fd-ba83ec41ddd1")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

