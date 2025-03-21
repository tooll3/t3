namespace Examples.user.wake.revision2021;

[Guid("eb13f8e8-0fe1-47f0-8e86-45f85cf2f0f6")]
 internal sealed class RevisionPanic : Instance<RevisionPanic>
{

    [Output(Guid = "bf45ca13-829b-46e4-ba46-886e3890c1be")]
    public readonly Slot<Texture2D> Output = new();

    [Output(Guid = "b5891c89-a8cf-4325-81be-f869b395e60c")]
    public readonly Slot<T3.Core.DataTypes.Command> Draws = new();


}