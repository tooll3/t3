using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e34c88f6_815e_4ce1_a6a8_59e2c8101849
{
    public class SubdivisionStretch : Instance<SubdivisionStretch>
    {
        [Output(Guid = "d8ec6fe5-ee96-4eaa-ba1f-05c67cdf0f0b")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "40bc83fb-a3a4-4bfd-b131-8ecf2908b1a3")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "8a571283-e4a6-4707-a8b9-a09b4781160a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> FxTextures = new();

        [Input(Guid = "639c698e-328b-4acd-ae48-bbd1ba32f31b")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new();

        [Input(Guid = "c91a5c04-34c8-4d89-8381-0c93066cc81d")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new();

        [Input(Guid = "2c310da5-3e00-48cb-a073-9b79324eca17")]
        public readonly InputSlot<float> Size = new();

        [Input(Guid = "71c99158-cf0e-4ceb-82ee-5ef5685441b3")]
        public readonly InputSlot<float> SubdivisionThreshold = new();

        [Input(Guid = "82a8fbb3-c1be-494e-8a8d-e7ccf5440556")]
        public readonly InputSlot<float> Padding = new();

        [Input(Guid = "3e164af6-1cb3-45b3-a319-e562789d73f7")]
        public readonly InputSlot<float> Feather = new();

        [Input(Guid = "5cae4d6e-d441-42f7-8e17-3aeb58719f08")]
        public readonly InputSlot<System.Numerics.Vector4> GapColor = new();

        [Input(Guid = "8597dfcf-5697-437d-91e2-664540766806")]
        public readonly InputSlot<float> MixOriginal = new();

        [Input(Guid = "32d082f5-d6e1-4068-bcd9-a01977ed72df")]
        public readonly InputSlot<int> MaxSubdivisions = new();

        [Input(Guid = "7a8684c9-ee81-49c1-ad13-d91d62799efb")]
        public readonly InputSlot<float> Randomize = new();
    }
}

