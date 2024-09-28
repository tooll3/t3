namespace Examples.user.wake.summer2024.scenes;

[Guid("74c9d0f5-146e-4dbd-9888-303aac958dbb")]
public class TheMoralQuestion : Instance<TheMoralQuestion>
{
    [Output(Guid = "9751cc49-be93-416f-96ae-b39fae9d959b")]
    public readonly Slot<Command> Output = new Slot<Command>();


}