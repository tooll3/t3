using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_12cbdef0_bcca_4bc7_96f5_d5806ca295af
{
    public class Scene12 : Instance<Scene12>
    {
        [Output(Guid = "b5cbda1b-d44f-4195-9ee4-f743b1591df7")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "2f952eff-24b7-4497-a52d-f4cd72a779d7")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new();


    }
}

