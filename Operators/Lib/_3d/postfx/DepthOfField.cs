namespace Lib._3d.postfx;

[Guid("53d3eebd-4ead-4965-b26d-10a8bbd48182")]
internal sealed class DepthOfField : Instance<DepthOfField>
{

    [Output(Guid = "a54cc25b-9ea2-4012-b462-16c565718cf8")]
    public readonly Slot<Texture2D> TextureOut = new();

    [Output(Guid = "07695aef-ea0d-4cdc-864f-dd2de8a947fb")]
    public readonly Slot<Texture2D> Depth = new();

    [Input(Guid = "bc1685a8-0a92-460f-85ca-7f096db965f0")]
    public readonly InputSlot<Texture2D> TextureBuffer = new();

    [Input(Guid = "c2e7ebf7-5056-4380-9a9f-850b350804c9")]
    public readonly InputSlot<Texture2D> DepthBuffer = new();

    [Input(Guid = "3655d507-96b3-4ded-9cef-886ea703ca89")]
    public readonly InputSlot<float> Amount = new();

    [Input(Guid = "97b76d02-e735-4e93-88ad-5c9b8766a63c")]
    public readonly InputSlot<float> FocusDistance = new();

    [Input(Guid = "1592e94b-a20d-463c-baec-5fb5dfa85532")]
    public readonly InputSlot<Vector4> BackgroundColor = new();

    [Input(Guid = "493c40f0-21e6-466b-afc2-eff570229c86")]
    public readonly InputSlot<int> MaxSamples = new();

    [Input(Guid = "40de51d8-91dd-461d-a7be-d4096313eec2")]
    public readonly InputSlot<Vector2> NearFarRange = new();

    [Input(Guid = "22f5e8db-0b80-47dc-b30b-4bc49d9fad59")]
    public readonly InputSlot<Command> Command = new();

}