using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1cdedb32_f23b_4649_b4d3_9e158ef9be40
{
    public class TVIntroSceneSetup : Instance<TVIntroSceneSetup>
    {
        [Output(Guid = "087b8484-23cc-45e8-9290-3a98f07bd87a")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        [Input(Guid = "4c5c6be2-05d0-4a70-8705-1c0aad83fa2d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> TvImage = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "7b9c9b05-4654-48c5-9ee4-8eff949f106f")]
        public readonly InputSlot<float> TransitionProgress = new InputSlot<float>();

        [Input(Guid = "b9b50526-af84-4a25-927d-a4b64f230edb")]
        public readonly InputSlot<float> MainIntensity = new InputSlot<float>();

        [Input(Guid = "95607fe6-5f9b-403b-bce9-e56bb2c64c48")]
        public readonly InputSlot<float> TV_ImageBrightness = new InputSlot<float>();

        [Input(Guid = "064c6835-6ecc-4bd3-afc6-25541a8c106c")]
        public readonly InputSlot<float> TV_GlitchAmount = new InputSlot<float>();

        [Input(Guid = "445d82b2-a33d-40fe-acf2-06b1bf69122c")]
        public readonly InputSlot<float> TV_DistortionNoise = new InputSlot<float>();

        [Input(Guid = "4a3bb4d3-89ad-4d1f-969d-6791c6384a41")]
        public readonly InputSlot<float> FX_Amount = new InputSlot<float>();


    }
}

