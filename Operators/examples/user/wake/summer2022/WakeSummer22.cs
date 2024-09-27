namespace examples.user.wake.summer2022;

[Guid("5f305acf-4e8f-4bc8-945a-cbf4f325a9cd")]
public class WakeSummer22 : Instance<WakeSummer22>
{
    [Output(Guid = "f98b4ef9-a376-4115-8f6c-6705442d79c3")]
    public readonly Slot<Texture2D> ColorBuffer = new();


}