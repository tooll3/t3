using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0c3a8cc9_85bf_4ded_b35b_7b447c7e13dd
{
    public class Scene13 : Instance<Scene13>
    {
        [Output(Guid = "1fa366e9-d82c-468d-b026-2e484a2b88a0")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "c790d267-bd75-4bc4-a6bb-e89bcccbe6ae")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new();


    }
}

