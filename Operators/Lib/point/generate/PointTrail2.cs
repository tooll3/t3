namespace Lib.point.generate;

[Guid("945014cf-ba0b-40b3-85f9-f7deed70fa8d")]
internal sealed class PointTrail2 : Instance<PointTrail2>
{

    [Output(Guid = "2a23b42c-ec03-401a-842a-6bdc0c633b7e")]
    public readonly Slot<BufferWithViews> OutBuffer = new();

    [Input(Guid = "f22a4834-6333-4ed5-b07d-237692c61dc6")]
    public readonly InputSlot<BufferWithViews> GPoints = new();

    [Input(Guid = "9a3998f9-f68a-4f8f-84dc-643e89f8c4f2")]
    public readonly InputSlot<int> TrailLength = new();

    [Input(Guid = "274f1a1f-4dfa-4426-b53e-77c0c96cf7d8")]
    public readonly InputSlot<bool> IsEnabled = new();

    [Input(Guid = "98366176-fdf3-42e1-afbc-a87fc0f9d82d")]
    public readonly InputSlot<bool> Reset = new();

    [Input(Guid = "a5448c00-a6d4-476e-bf41-826fdff531cc")]
    public readonly InputSlot<float> AddSeperatorThreshold = new();
}