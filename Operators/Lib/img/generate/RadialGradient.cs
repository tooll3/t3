using T3.Core.Utils;

namespace Lib.img.generate;

[Guid("82ad8911-c930-4851-803d-3f24422445bc")]
internal sealed class RadialGradient : Instance<RadialGradient>
{
    [Output(Guid = "9785937a-2b8f-4b2e-92ac-98ec067a40f2")]
    public readonly Slot<Texture2D> TextureOutput = new();

    [Input(Guid = "54bca43c-fc2b-4a40-b991-8b76e35eee01")]
    public readonly InputSlot<Texture2D> Image = new InputSlot<Texture2D>();

    [Input(Guid = "3f5a284b-e2f0-47e2-bf79-2a7fe8949519")]
    public readonly InputSlot<Gradient> Gradient = new InputSlot<Gradient>();

    [Input(Guid = "bfdcfed4-263f-4115-a1a8-291088e34c0a")]
    public readonly InputSlot<float> Width = new InputSlot<float>();

    [Input(Guid = "98314ae6-b2a9-433b-90e9-931b059ae62e")]
    public readonly InputSlot<float> Offset = new InputSlot<float>();

    [Input(Guid = "5e93ba7d-a33d-4574-a438-a13c03361c29")]
    public readonly InputSlot<Vector2> BiasAndGain = new InputSlot<Vector2>();

    [Input(Guid = "6c1dc695-1c0a-47fe-aea1-e3abec904883")]
    public readonly InputSlot<bool> PingPong = new InputSlot<bool>();

    [Input(Guid = "eab31c38-0e6f-432a-9f15-04bfb0aae28c")]
    public readonly InputSlot<bool> Repeat = new InputSlot<bool>();

    [Input(Guid = "1cf83367-7a34-4369-86d8-77dd4fe48d63")]
    public readonly InputSlot<Vector2> Center = new InputSlot<Vector2>();

    [Input(Guid = "dc383dbd-9dab-4bb2-8c6e-7f094e28d8a9")]
    public readonly InputSlot<bool> PolarOrientation = new InputSlot<bool>();

    [Input(Guid = "7270a7df-744e-4b66-8f85-71fbdf0848d6", MappedType = typeof(SharedEnums.RgbBlendModes))]
    public readonly InputSlot<int> BlendMode = new InputSlot<int>();

    [Input(Guid = "cf2e1698-f996-4b83-8b59-3150e75d59c6")]
    public readonly InputSlot<Int2> Resolution = new InputSlot<Int2>();
}