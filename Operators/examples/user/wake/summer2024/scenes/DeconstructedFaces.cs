namespace Examples.user.wake.summer2024.scenes;

[Guid("0b8e41cf-4692-4522-8491-601fa1851a24")]
public class DeconstructedFaces : Instance<DeconstructedFaces>
{
    [Output(Guid = "02e02b5e-5a2f-4892-a229-24fab5d8683b")]
    public readonly Slot<Command> Output = new Slot<Command>();

    [Input(Guid = "e741b3d3-c9b5-4ed1-a2c4-3cb72389ac4a")]
    public readonly InputSlot<bool> SlowBass_x025 = new InputSlot<bool>();

    [Input(Guid = "72270725-1740-4bb5-9726-3aa76efed243")]
    public readonly InputSlot<bool> ClapTrigger2x = new InputSlot<bool>();

    [Input(Guid = "c7380ed5-23ac-4980-b27a-a353955d7939")]
    public readonly InputSlot<bool> KlakKlakKlakTrigger_4x = new InputSlot<bool>();

    [Input(Guid = "ba245fa9-608d-4ac3-a0f6-ad9b3514c665")]
    public readonly InputSlot<bool> FastSubBass_16x = new InputSlot<bool>();


}