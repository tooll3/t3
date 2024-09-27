namespace examples.lib._3d.mesh;

[Guid("70e97e6b-3ddf-4a88-b080-c63fdbd251c9")]
public class UVsViewerExample : Instance<UVsViewerExample>
{
    [Output(Guid = "7c291a16-1e48-40c6-9fbc-cab14bb80720")]
    public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


}