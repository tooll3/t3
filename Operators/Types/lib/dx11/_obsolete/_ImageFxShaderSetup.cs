using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_86cb0f9d_756e_4172_a886_90d56befb89b
{
    public class _ImageFxShaderSetup : Instance<_ImageFxShaderSetup>
    {
        [Output(Guid = "d84fc912-bc6a-4bff-a83f-be92b6ad0d57")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        
        [Input(Guid = "48142c54-b288-40f7-bb29-53554b45b118")]
        public readonly InputSlot<string> Source = new InputSlot<string>();

        [Input(Guid = "6c41b633-d781-4020-94de-3f202534b021")]
        public readonly InputSlot<string> EntryPoint = new InputSlot<string>();

        [Input(Guid = "3cb19166-97d3-4404-9a7b-1e96e4b326f0")]
        public readonly InputSlot<string> DebugName = new InputSlot<string>();

        [Input(Guid = "900877d3-c5df-420b-a2ac-eeb6c5219dd3")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();
        
        [Input(Guid = "4f73a058-1750-434b-b8b1-205c52d90c34")]
        public readonly MultiInputSlot<float> Params = new MultiInputSlot<float>();

    }
}

