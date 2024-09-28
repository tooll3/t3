namespace Examples.user.wake.summer2024.scenes;

[Guid("47da8125-359e-40b7-8cf6-1880c51147f6")]
public class TheTruth : Instance<TheTruth>
{
    [Output(Guid = "fffca69d-5975-46ac-ae01-855b354a2e57")]
    public readonly Slot<Command> Output = new Slot<Command>();

    [Input(Guid = "bda603a7-f97f-4c4f-87e1-65f5404863b9")]
    public readonly InputSlot<bool> HitNote = new InputSlot<bool>();

    [Input(Guid = "1f5f95be-770c-4ef0-8232-d9596337937e")]
    public readonly InputSlot<bool> VerySlowHit = new InputSlot<bool>();


}