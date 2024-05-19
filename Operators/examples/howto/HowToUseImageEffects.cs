using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_649d4542_4165_4129_9484_dd8fb8663e15
{
    public class HowToUseImageEffects : Instance<HowToUseImageEffects>
    {
        [Output(Guid = "30c39c3d-96cd-40ed-ae05-8b2a3eb6f177")]
        public readonly Slot<Texture2D> Texture = new();


    }
}

