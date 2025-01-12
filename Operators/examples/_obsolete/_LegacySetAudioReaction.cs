namespace Examples._obsolete;

[Guid("dd00cb1b-1c0e-4e79-9ea2-4b23686c6f37")]
internal sealed class _LegacySetAudioReaction : Instance<_LegacySetAudioReaction>
{
    [Output(Guid = "aab8fbed-1ffa-4d5b-b488-a458e45844d0")]
    public readonly Slot<Command> Output = new();


    [Input(Guid = "d243af8f-7b1d-4110-912b-2c430726becc")]
    public readonly MultiInputSlot<Command> Command = new();

    [Input(Guid = "a8900010-bf6d-44e4-bd72-28427a5dadd8")]
    public readonly InputSlot<int> InputSource = new();

}