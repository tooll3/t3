namespace Lib.img.fx.@_;

[Guid("1ba08d52-c8ec-479a-8dc0-95d92da36577")]
public class ColorGradeConstBuffer : Instance<ColorGradeConstBuffer>
{
    [Output(Guid = "c63a8582-726d-4a18-a256-48ccf13f1289", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
    public readonly Slot<Buffer> Buffer = new();

    public ColorGradeConstBuffer()
    {
        Buffer.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        var bufferContent = new ParamBufferLayout(Param1.GetValue(context), Param2.GetValue(context), Param3.GetValue(context), Param4.GetValue(context));
        ResourceManager.SetupConstBuffer(bufferContent, ref Buffer.Value);
        Buffer.Value.DebugName = nameof(ColorGradeConstBuffer);
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct ParamBufferLayout
    {
        public ParamBufferLayout(Vector4 param1, Vector4 param2, Vector4 param3, Vector4 param4)
        {
            Param1 = param1;
            Param2 = param2;
            Param3 = param3;
            Param4 = param4;
        }

        [FieldOffset(0*4)]
        public Vector4 Param1;
        [FieldOffset(4*4)]
        public Vector4 Param2;
        [FieldOffset(8*4)]
        public Vector4 Param3;
        [FieldOffset(12*4)]
        public Vector4 Param4;
    }       

    [Input(Guid = "7834D068-E09F-4A10-8FCA-AE73692836FC")]
    public readonly InputSlot<Vector4> Param1 = new();
    [Input(Guid = "49B2F2C5-C170-4AFC-99CA-0487D2FA66E4")]
    public readonly InputSlot<Vector4> Param2 = new();
    [Input(Guid = "E0E761AF-2541-47E1-9BBC-4364489A5334")]
    public readonly InputSlot<Vector4> Param3 = new();
    [Input(Guid = "D2C37218-81B7-45F5-AD56-DADDA3C37197")]
    public readonly InputSlot<Vector4> Param4 = new();
}