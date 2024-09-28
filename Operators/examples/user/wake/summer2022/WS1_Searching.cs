namespace Examples.user.wake.summer2022;

[Guid("90b20942-810b-480c-a19e-a41296cac9e6")]
 internal sealed class WS1_Searching : Instance<WS1_Searching>
{
    [Output(Guid = "8902091c-9eee-4a4e-a55e-67768ba3465a")]
    public readonly Slot<Command> Output = new();

    [Input(Guid = "e75b329f-26ae-4070-91bc-450c30d7453d")]
    public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new();


}