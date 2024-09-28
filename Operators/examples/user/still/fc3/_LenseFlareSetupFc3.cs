namespace Examples.user.still.fc3;

[Guid("6d594c55-a180-4742-8182-20b38929bab5")]
public class _LenseFlareSetupFc3 : Instance<_LenseFlareSetupFc3>
{
    [Output(Guid = "476d24e7-2e66-48fe-bde3-8c6e12a44492")]
    public readonly Slot<Command> Output = new();

    [Input(Guid = "7898640b-e30e-476f-8ab0-9310c00b513a")]
    public readonly InputSlot<float> Brightness = new();

    [Input(Guid = "3adb11f9-2a4a-4be7-8da4-460c41845f71")]
    public readonly InputSlot<int> RandomSeed = new();

    [Input(Guid = "ebd05e33-63e1-47f4-ae8a-62ce5dcd2ce6")]
    public readonly InputSlot<int> LightIndex = new();

    [Input(Guid = "0bbb7834-b4ca-4ac5-9a70-e29e1d646efd")]
    public readonly InputSlot<System.Numerics.Vector4> RandomizeColor = new();

    [Input(Guid = "ed497b10-53a4-46f1-a5d9-a2e1d988c5b9")]
    public readonly InputSlot<float> CoreBrightness = new();


}