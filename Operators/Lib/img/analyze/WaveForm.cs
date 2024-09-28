namespace Lib.img.analyze;

[Guid("92e28e50-bd40-4f93-ba92-8f69cded6ec1")]
public class WaveForm : Instance<WaveForm>
{
    [Output(Guid = "d81d0fd1-3ba0-4576-8fea-d37fb6ec5548")]
    public readonly Slot<Texture2D> ImgOutput = new();

    [Input(Guid = "73afdd61-dd59-4018-a473-025272deab93")]
    public readonly InputSlot<Texture2D> EffectTexture = new();

    [Input(Guid = "cf46bbff-86cc-43db-a712-1ca0a90d34a1")]
    public readonly InputSlot<float> Height = new();

    [Input(Guid = "f975a9bb-26db-4683-abe7-88e900787610")]
    public readonly InputSlot<float> Opacity = new();

    [Input(Guid = "381d9aa5-2685-4509-9640-1dcca64746fa")]
    public readonly InputSlot<float> DimBackground = new();

    [Input(Guid = "d86c228c-f586-471e-ab07-4414375de29a")]
    public readonly InputSlot<float> ColorIntensity = new();

    [Input(Guid = "3a226812-98bf-4fe9-b435-f84b154a41e8")]
    public readonly InputSlot<Int2> OverrideSize = new();

    [Input(Guid = "5ce4b7d6-ecaf-44d9-aa67-74892339cb06")]
    public readonly InputSlot<float> EnlargeVectorScopeCenter = new();

    [Input(Guid = "c3eb1998-3bbf-436c-bde8-9fbbf4f56e54")]
    public readonly InputSlot<bool> ShowVectorscope = new InputSlot<bool>();


}