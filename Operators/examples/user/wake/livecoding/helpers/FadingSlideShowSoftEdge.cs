namespace Operators.examples.user.wake.livecoding.helpers;

[Guid("92750028-5fac-4e6d-84fe-f779f6d0ae33")]
public class FadingSlideShowSoftEdge : Instance<FadingSlideShowSoftEdge>
{
    [Output(Guid = "dbfb113d-8d9e-481c-8048-2bd069f3b4b8")]
    public readonly Slot<Command> Output = new();

    [Output(Guid = "08be8ce0-ecc2-4c9c-9a7d-aacf77848494")]
    public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

    [Input(Guid = "34c327a8-d77b-46cd-b3b4-bc09a1397004")]
    public readonly InputSlot<float> IndexAndFraction = new();

    [Input(Guid = "0429a8b8-ecb1-48fb-acef-e98a155dedf3")]
    public readonly InputSlot<float> BlendSpeed = new();

    [Input(Guid = "38b5f4da-b6b0-4b0c-a2a6-e66b7db7f257")]
    public readonly InputSlot<float> Scale = new();

    [Input(Guid = "7d9b6e41-e1af-4359-9885-11c4b1d398d2")]
    public readonly InputSlot<System.Numerics.Vector2> Position = new();

    [Input(Guid = "0260ba8e-7dd4-4d2a-89df-2f17192aecbf")]
    public readonly InputSlot<float> RandomOffset = new();

    [Input(Guid = "6c52d99d-9361-4d99-b9b0-4397e564ca19")]
    public readonly InputSlot<string> FolderWithImages = new();

    [Input(Guid = "4c268191-1350-4dc5-96ff-7f7b3ad4aeff")]
    public readonly InputSlot<System.Numerics.Vector4> Color = new();

    [Input(Guid = "57eea5bc-49fe-4b07-b817-edaaff2b1c57")]
    public readonly InputSlot<System.Numerics.Vector4> BackgroundColor = new();

    [Input(Guid = "5ddd4bdd-c231-416c-ad25-0fc73bcab016")]
    public readonly InputSlot<int> ScaleMode = new();

    [Input(Guid = "0897d439-f170-4983-ad8b-05e416709918")]
    public readonly InputSlot<bool> TriggerUpdate = new InputSlot<bool>();

    [Input(Guid = "adcfe5c9-e355-439b-bde0-b4bfe3c37d3e")]
    public readonly InputSlot<int> FadeType = new InputSlot<int>();

}