namespace Lib.image.analyze;

[Guid("592a2b6f-d4e3-43e0-9e73-034cca3b3900")]
internal sealed class ImageLevels : Instance<ImageLevels>
{
    [Output(Guid = "ae9ebfa0-3528-489b-9c07-090f26dd6968")]
    public readonly Slot<Texture2D> Output = new();

    [Input(Guid = "f434bac8-b7d8-4787-adf2-1782d6588da8")]
    public readonly InputSlot<Texture2D> Texture2d = new();

    [Input(Guid = "1224b62e-5fca-41e9-a388-4c13c1458d56")]
    public readonly InputSlot<Vector2> Center = new();

    [Input(Guid = "48e80f45-9685-4ded-aa1c-d05e16658c5a")]
    public readonly InputSlot<float> Width = new();

    [Input(Guid = "f1084d72-f8b8-4723-82be-e1e98880faf3")]
    public readonly InputSlot<float> Rotation = new();

    [Input(Guid = "a8a4d660-7356-40de-8dc6-549a72b69973")]
    public readonly InputSlot<float> ShowOriginal = new();
}