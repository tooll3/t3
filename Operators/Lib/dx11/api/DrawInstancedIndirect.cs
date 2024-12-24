namespace Lib.dx11.api;

[Guid("c676b9c7-06d7-4ee0-8ffc-9bee96c5dc18")]
internal sealed class DrawInstancedIndirect: Instance<DrawInstancedIndirect>
{
    [Output(Guid = "3A8880AF-BBBF-4560-B0C7-6E643A20FC20", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
    public readonly Slot<Command> Output = new();

    public DrawInstancedIndirect()
    {
        Output.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        Buffer buffer = Buffer.GetValue(context);
        if (buffer == null)
            return;
            
        var device = ResourceManager.Device;
        var deviceContext = device.ImmediateContext;
        deviceContext.DrawInstancedIndirect(buffer, AlignedByteOffsetForArgs.GetValue(context));
    }

    [Input(Guid = "6C87816C-DA1D-4429-A1F6-61233AA3D7B1")]
    public readonly InputSlot<Buffer> Buffer = new InputSlot<Buffer>();
    [Input(Guid = "BC874135-45F2-45E2-8005-244B9123ED20")]
    public readonly InputSlot<int> AlignedByteOffsetForArgs = new();
}