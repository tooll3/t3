namespace Examples.user.still.there.research;

[Guid("8adede33-fbdd-4ee9-b76e-cf2af28999c5")]
internal sealed class PartialPhysarum : Instance<PartialPhysarum>
{

    [Output(Guid = "873d1725-af23-4ba9-b3ad-d538d49d437b")]
    public readonly TimeClipSlot<Command> Output3 = new();


}