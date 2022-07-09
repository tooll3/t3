using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fa20eada_f34b_4137_a591_1605a6c1a628
{
    public class FishyScene : Instance<FishyScene>
    {
        [Output(Guid = "a9674e19-bc33-4197-8c93-daca15406dca")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

