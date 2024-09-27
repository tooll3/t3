namespace examples.user.still.fc3_noloops;

[Guid("88aa2a40-5841-48b7-8e95-907ee6b70270")]
public class FC3_NoLoops : Instance<FC3_NoLoops>
{
    [Output(Guid = "2f3ed78b-7372-47a5-a486-4c3831ee0a94")]
    public readonly Slot<Texture2D> ImgOutput = new();


}