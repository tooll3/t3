namespace Lib.img.fx;

[Guid("1b149f1f-529c-4418-ac9d-3871f24a9e38")]
public class Displace : Instance<Displace>
{
    [Output(Guid = "0faa056c-b1d6-4e1f-a9be-b0791f3bae84")]
    public readonly Slot<Texture2D> Output = new();

    [Input(Guid = "d0508dfa-89cf-4713-8f5e-893dd5bfc3f4")]
    public readonly InputSlot<Texture2D> Image = new();

    [Input(Guid = "3b5b278d-fd4e-4216-9916-5cd7ffd54ab2")]
    public readonly InputSlot<Texture2D> DisplaceMap = new();

    [Input(Guid = "0f2867ab-a65e-4bf3-b1b5-9c241690ba5f")]
    public readonly InputSlot<float> Displacement = new();

    [Input(Guid = "0eff3a75-eafc-4a5e-8a2c-10577c12e776")]
    public readonly InputSlot<float> DisplacementOffset = new();

    [Input(Guid = "dc8dfa33-1a49-4800-8c1f-89b29d7427f3")]
    public readonly InputSlot<float> Twist = new();

    [Input(Guid = "77673c64-918d-46a6-aa29-c362057afee6")]
    public readonly InputSlot<float> Shade = new();

    [Input(Guid = "d430113e-33fd-49b7-b1c1-32cc95154a4e")]
    public readonly InputSlot<int> SampleCount = new();

    [Input(Guid = "c13b83ce-7dd2-4b4e-bf19-b994493b92a3")]
    public readonly InputSlot<float> DisplaceMapSampling = new();

    [Input(Guid = "b9dd77ff-7e5a-4036-a1c9-24501f1d4d9d")]
    public readonly InputSlot<float> SampleSpread = new();

    [Input(Guid = "6978ceab-98a9-4258-9c67-db41173d2aae")]
    public readonly InputSlot<float> SampleOffset = new();

    [Input(Guid = "1ff7d454-b8fb-470f-beee-ec7521db8a7f")]
    public readonly InputSlot<Vector2> DisplaceMapOffset = new();

    [Input(Guid = "6e772174-813d-4baa-b6b5-27e197b547ac")]
    public readonly InputSlot<TextureAddressMode> WrapMode = new();

    [Input(Guid = "6a5c120f-7c04-439b-ad2d-6f78ceb3b378", MappedType = typeof(DisplaceModes))]
    public readonly InputSlot<int> DisplaceMode = new();

    [Input(Guid = "b2f58dc7-e5c6-4c57-a704-94aaa0b1e002")]
    public readonly InputSlot<bool> GenerateMips = new();
        
    private enum DisplaceModes {
        IntensityGradient,
        Intensity,
        NormalMap,
        SignedNormalMap,
    }
}