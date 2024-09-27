namespace user.pixtur.examples;

[Guid("6ef2c96c-0ade-4dfb-bfe1-d852cf453a0e")]
public class DrawPointsExample : Instance<DrawPointsExample>
{
    [Output(Guid = "246af894-54ae-4863-a102-c6cc5e56aa7c")]
    public readonly Slot<Texture2D> ImgOutput = new();


}