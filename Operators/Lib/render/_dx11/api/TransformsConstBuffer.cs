using T3.Core.Rendering;

namespace Lib.render._dx11.api;

[Guid("a60adc26-d7c6-4615-af78-8d2d6da46b79")]
internal sealed class TransformsConstBuffer : Instance<TransformsConstBuffer>
{
    [Output(Guid = "7A76D147-4B8E-48CF-AA3E-AAC3AA90E888", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
    public readonly Slot<Buffer> Buffer = new();

    public TransformsConstBuffer()
    {
        Buffer.UpdateAction += Update;
    }

    private void Update(EvaluationContext context)
    {
        //Log.Debug($" ObjectToWorld: {context.ObjectToWorld}", this);
        var bufferContent = new TransformBufferLayout(context.CameraToClipSpace, context.WorldToCamera, context.ObjectToWorld);
        ResourceManager.SetupConstBuffer(bufferContent, ref Buffer.Value);
        Buffer.Value.DebugName = nameof(TransformsConstBuffer);
    }
}