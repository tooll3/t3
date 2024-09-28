namespace Examples.user.wake.revision2021;

[Guid("e7821087-ec80-4c1c-907a-e3506dd345b3")]
 internal sealed class RevRibbons : Instance<RevRibbons>
{

    [Output(Guid = "f916f73c-ef64-40b5-9994-04292e6a0a9a")]
    public readonly TimeClipSlot<Command> Output2 = new();


}