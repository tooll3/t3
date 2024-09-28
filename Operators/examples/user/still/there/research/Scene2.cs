namespace Examples.user.still.there.research;

[Guid("0f0488f7-7f1a-4464-a6b1-86bc52a4b217")]
internal sealed class Scene2 : Instance<Scene2>
{
    [Output(Guid = "ea2488f2-fa25-4e97-a8fd-96f8b62b51bb")]
    public readonly Slot<Texture2D> TextureOutput = new();

    [Output(Guid = "a89081d9-5fff-4603-a974-2318f17913a5")]
    public readonly Slot<Texture2D> DepthBuffer = new();


}