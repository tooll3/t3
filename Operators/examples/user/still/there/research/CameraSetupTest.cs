namespace examples.user.still.there.research;

[Guid("ef88a1f2-a541-4c7d-a6c7-ce6f768db2f5")]
public class CameraSetupTest : Instance<CameraSetupTest>
{
    [Output(Guid = "13147234-971e-41b1-adee-757f4e2151e0")]
    public readonly Slot<Command> Output = new();


}