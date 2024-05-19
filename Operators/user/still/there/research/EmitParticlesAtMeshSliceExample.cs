using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ccafae36_6001_4ee8_b0b5_76c1adebcdde
{
    public class EmitParticlesAtMeshSliceExample : Instance<EmitParticlesAtMeshSliceExample>
    {
        [Output(Guid = "c50025cd-95d0-4aa4-b9fe-6088b5c9cda6")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "9c7f20a9-5b1a-42c9-ab98-22cc1a9552c9")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new();


    }
}

