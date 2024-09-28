namespace Examples.user.wake.summer2024;

[Guid("9fb0c9a1-5d76-47bc-8749-17aa8d13e4e4")]
public class _TidalDebugGizmo : Instance<_TidalDebugGizmo>
{
    [Output(Guid = "0840d8d0-888d-40df-8941-cd920b1e4578")]
    public readonly Slot<Command> Output = new Slot<Command>();

    [Output(Guid = "2fda9526-5bd7-4869-bf2b-926190a63239")]
    public readonly Slot<bool> WasHit = new Slot<bool>();

    [Output(Guid = "61bb679e-a687-4116-866c-b982f376d8d7")]
    public readonly Slot<float> Note = new Slot<float>();

    [Input(Guid = "ff44cec1-765d-420b-8c3c-77f971b5f50b")]
    public readonly InputSlot<T3.Core.DataTypes.Dict<float>> DictionaryInput = new InputSlot<T3.Core.DataTypes.Dict<float>>();

    [Input(Guid = "13a40794-c09a-4d7d-8f1d-b87945d48f14")]
    public readonly InputSlot<string> Id = new InputSlot<string>();

    [Input(Guid = "e605ec18-38bb-4d81-8100-69582e6734e9")]
    public readonly InputSlot<string> Midi = new InputSlot<string>();

    [Input(Guid = "11a66dbf-000d-4241-bc67-1acb7a3771d2")]
    public readonly InputSlot<bool> UseNotesForBeats = new InputSlot<bool>();

    [Input(Guid = "ba924ff3-b386-42f9-b2b1-a408a5d1c66a")]
    public readonly InputSlot<string> Name = new InputSlot<string>();

    [Input(Guid = "656303da-90bf-4790-84e8-7b9f5aac2ebd")]
    public readonly InputSlot<string> Notes = new InputSlot<string>();

    [Input(Guid = "2a0e7afe-8903-44c7-b99d-399d7a6243e1")]
    public readonly InputSlot<bool> LogDebug = new InputSlot<bool>();

}