using lib.dx11.buffer;
using T3.Core.Rendering;

namespace lib._3d.rendering._;

[Guid("843c9378-6836-4f39-b676-06fd2828af3e")]
public class GetCamTransformBuffer : Instance<GetCamTransformBuffer>
{
    [Output(Guid = "FB108D2D-04B0-427D-888D-79EB7EBF1E96", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
    public readonly Slot<Buffer> Buffer = new();

    [Output(Guid = "8EDC2DB1-A214-4B77-A334-FA4BF1FF1AB7", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
    public readonly Slot<Buffer> PreviousBuffer = new();
        
    public GetCamTransformBuffer()
    {
        Buffer.UpdateAction += Update;
            
    }
        
    private void Update(EvaluationContext context)
    {
        if (!CameraReference.IsConnected)
        {
            CameraReference.DirtyFlag.Clear();
            return;
        }

        var obj = CameraReference.GetValue(context);
        if (obj == null)
        {
            Log.Warning("Camera reference is undefined", this);
            return;
        }

        if (obj is not ICameraPropertiesProvider camera)
        {
            Log.Warning("Can't GetCamProperties from invalid reference type", this);
            return;
        }

        if (_previousBufferInitialized)
        {
            ResourceManager.SetupConstBuffer(_bufferContent, ref PreviousBuffer.Value);
            PreviousBuffer.Value.DebugName=nameof(TransformsConstBuffer);
            PreviousBuffer.DirtyFlag.Clear();
        }
            
        _bufferContent = new TransformBufferLayout(camera.CameraToClipSpace, camera.WorldToCamera, camera.LastObjectToWorld);
        ResourceManager.SetupConstBuffer(_bufferContent, ref Buffer.Value);
        Buffer.Value.DebugName=nameof(TransformsConstBuffer);
        _previousBufferInitialized = true;
    }

    [Input(Guid = "A3190889-5473-4870-97CF-93E6CF94132B")]
    public readonly InputSlot<Object> CameraReference = new();

        
    private TransformBufferLayout _bufferContent;
    private bool _previousBufferInitialized;
}