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

        [Input(Guid = "4a3bb4d3-89ad-4d1f-969d-6791c6384a41")]
        public readonly InputSlot<float> FX_Amount = new InputSlot<float>();

        [Input(Guid = "b9b50526-af84-4a25-927d-a4b64f230edb")]
        public readonly InputSlot<float> MainIntensity = new InputSlot<float>();

        [Input(Guid = "92d469d0-33ed-41e5-9b47-d20f58e38e91")]
        public readonly InputSlot<float> SceneBrightness = new InputSlot<float>();

        [Input(Guid = "b8191816-955d-4152-bc61-cf1d33351f22")]
        public readonly InputSlot<bool> Bypass = new InputSlot<bool>();


    }
}

