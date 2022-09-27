using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ade1d03d_db80_41ad_bcfa_8a2b900e9d41
{
    public class DepthToLinear : Instance<DepthToLinear>
    {
        [Output(Guid = "eff29dae-87c5-43a4-856b-51ac3abf567a")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "de65c36d-10a7-446f-a4dd-e55ce42f4203")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "a5f6347a-9c57-46f2-be39-80499b35cdf7")]
        public readonly InputSlot<float> Near = new InputSlot<float>();

        [Input(Guid = "9f42b73c-d6f1-4907-ba55-9fb56514aa23")]
        public readonly InputSlot<float> Far = new InputSlot<float>();

        [Input(Guid = "50dbf432-ea4d-4d49-8cf4-e946a950b998")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> OutputTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

    }
}

