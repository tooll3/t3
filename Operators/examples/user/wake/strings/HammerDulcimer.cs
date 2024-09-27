namespace examples.user.wake.strings;

[Guid("d1432885-5d16-49ec-afb9-845d0f3efcb8")]
public class HammerDulcimer : Instance<HammerDulcimer>
{

    [Output(Guid = "5eb793e9-e004-4759-bd18-61ec7dce40a4")]
    public readonly TimeClipSlot<Command> Output2 = new();


}