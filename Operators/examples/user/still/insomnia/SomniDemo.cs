namespace examples.user.still.insomnia;

[Guid("f05356b6-3456-4a1b-988c-0c8f89fb4816")]
public class SomniDemo : Instance<SomniDemo>
{
    [Output(Guid = "2297f9a6-4c96-45c7-be81-0f7d048428d3")]
    public readonly Slot<Texture2D> TextureOutput = new();


}