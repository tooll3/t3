namespace Examples.user.wake.summer2022.elements;

[Guid("814acb53-9a96-476f-b580-6eef174a318b")]
 internal sealed class WakeArps : Instance<WakeArps>
{
    [Output(Guid = "e2301a1a-df21-42b2-88ec-5c5f4c705809")]
    public readonly Slot<Command> Output = new();


}