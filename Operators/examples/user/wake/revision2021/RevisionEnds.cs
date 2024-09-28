namespace Examples.user.wake.revision2021;

[Guid("fb5e9e3c-6ded-4e62-b456-28c8d5b29a1d")]
 internal sealed class RevisionEnds : Instance<RevisionEnds>
{

    [Output(Guid = "3ea37eb9-446c-4078-bb0c-a602036233e3")]
    public readonly TimeClipSlot<Command> Output2 = new();


}