namespace Types.Gfx;

[Guid("b6c5be1d-b133-45e9-a269-8047ea0d6ad7")]
public sealed class StructuredBufferWithViews : Instance<StructuredBufferWithViews>
{

    [Output(Guid = "c997268d-6709-49de-980e-64d7a47504f7")]
    public readonly Slot<BufferWithViews> BufferWithViews = new();

    public StructuredBufferWithViews()
    {
        BufferWithViews.UpdateAction += UpdateBuffer;
    }

    private void UpdateBuffer(EvaluationContext context)
    {
        var stride = Stride.GetValue(context);
        var sizeInBytes = stride * Count.GetValue(context);
        var createSrv = CreateSrv.GetValue(context);
        var createUav = CreateUav.GetValue(context);
        var uavBufferFlags = BufferFlags.GetValue(context);

        if (sizeInBytes <= 0)
        {
            BufferWithViews.Value = null;
            return;
        }

        BufferWithViews.Value ??= new BufferWithViews();

        ResourceManager.SetupStructuredBuffer(sizeInBytes, stride, ref BufferWithViews.Value.Buffer);

        if (createSrv)
            ResourceManager.CreateStructuredBufferSrv(BufferWithViews.Value.Buffer, ref BufferWithViews.Value.Srv);

        if (createUav)
            ResourceManager.CreateStructuredBufferUav(BufferWithViews.Value.Buffer, uavBufferFlags, ref BufferWithViews.Value.Uav);
    }
        
    [Input(Guid = "16f98211-fe97-4235-b33a-ddbbd2b5997f")]
    public readonly InputSlot<int> Count = new();

    [Input(Guid = "0016dd87-8756-4a97-a0da-096e1a879c05")]
    public readonly InputSlot<int> Stride = new();


    [Input(Guid = "bb5fa9b9-1155-47f5-9ed5-7832826f3df2")]
    public readonly InputSlot<bool> CreateSrv = new();

    [Input(Guid = "dd0db46d-a6a0-4d84-9dd4-ab805e2197fb")]
    public readonly InputSlot<bool> CreateUav = new();
        
    [Input(Guid = "43c2b314-4809-4022-9b07-99965e5c1a7a")]
    public readonly InputSlot<UnorderedAccessViewBufferFlags> BufferFlags = new();
}