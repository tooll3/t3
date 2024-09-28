namespace Examples.user._1x;

[Guid("e830b91b-80cc-4fc7-9b9f-52b47c95fc85")]
public class TVIntroSceneChair : Instance<TVIntroSceneChair>
{
    [Output(Guid = "6c4c601b-7018-4a61-b72b-6bce5f336b32")]
    public readonly Slot<Command> Output = new();


}