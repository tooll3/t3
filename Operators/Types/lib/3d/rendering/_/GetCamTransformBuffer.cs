using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Core.Resource;
using T3.Operators.Types.Id_a60adc26_d7c6_4615_af78_8d2d6da46b79;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_843c9378_6836_4f39_b676_06fd2828af3e
{
    public class GetCamTransformBuffer : Instance<GetCamTransformBuffer>
    {
        [Output(Guid = "FB108D2D-04B0-427D-888D-79EB7EBF1E96", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Buffer> Buffer = new();

        [Output(Guid = "8EDC2DB1-A214-4B77-A334-FA4BF1FF1AB7", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Buffer> PreviousBuffer = new();
        
        public GetCamTransformBuffer()
        {
            Buffer.UpdateAction = Update;
            
        }
        
        private void Update(EvaluationContext context)
        {
            if (!CameraReference.HasInputConnections)
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
            //camera.CameraToClipSpace, camera.WorldToCamera, camera.LastObjectToWorld
            //_bufferContent = new TransformBufferLayout(context);
            ResourceManager.SetupConstBuffer(_bufferContent, ref Buffer.Value);
            Buffer.Value.DebugName=nameof(TransformsConstBuffer);
            _previousBufferInitialized = true;
        }

        [Input(Guid = "A3190889-5473-4870-97CF-93E6CF94132B")]
        public readonly InputSlot<Object> CameraReference = new();

        
        private TransformBufferLayout _bufferContent;
        private bool _previousBufferInitialized;
    }
}

