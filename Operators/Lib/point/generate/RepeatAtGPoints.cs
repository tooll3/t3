namespace Lib.point.generate;

[Guid("780edb20-f83f-494c-ab17-7015e2311250")]
internal sealed class RepeatAtGPoints : Instance<RepeatAtGPoints>
{

    [Output(Guid = "3ac76b2a-7b1c-4762-a3f6-50529cd42fa8")]
    public readonly Slot<BufferWithViews> OutBuffer = new();

    [Input(Guid = "a952d91a-a86b-4370-acd9-e17b19025966")]
    public readonly InputSlot<BufferWithViews> GTargets = new();

    [Input(Guid = "47c3c549-78bb-41fd-a88c-58f643870b40")]
    public readonly InputSlot<BufferWithViews> GPoints = new();

    [Input(Guid = "f15a003c-7969-4505-b598-6c6c4b5a3bbe")]
    public readonly InputSlot<bool> ApplyTargetOrientation = new();

    [Input(Guid = "f71ddebe-1f2c-47d0-ba39-eb5c4693e909")]
    public readonly InputSlot<bool> ApplyTargetScaleW = new();

    [Input(Guid = "f582aa39-f5e0-46ad-89ae-6f29ab60d3e6")]
    public readonly InputSlot<bool> MultiplyTargetW = new();

    [Input(Guid = "9df1f57c-a079-49c1-b537-d8eb08f2d0d3")]
    public readonly InputSlot<float> Scale = new();

    [Input(Guid = "796d3d55-32b3-436e-a4c3-f15e1585a914", MappedType = typeof(ConnectionModes))]
    public readonly InputSlot<int> ConnectLines = new();

    [Input(Guid = "6026d26d-b958-4508-b543-92fbdf8950d6")]
    public readonly InputSlot<bool> AddSeparators = new();


    private enum ConnectionModes
    {
        Linear,
        Interwoven,
    }
}