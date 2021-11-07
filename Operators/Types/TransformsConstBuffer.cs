using System.Runtime.InteropServices;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_a60adc26_d7c6_4615_af78_8d2d6da46b79
{
    public class TransformsConstBuffer : Instance<TransformsConstBuffer>
    {
        [Output(Guid = "7A76D147-4B8E-48CF-AA3E-AAC3AA90E888", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        public TransformsConstBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var bufferContent = new BufferLayout(context.CameraToClipSpace, context.WorldToCamera, context.ObjectToWorld);
            ResourceManager.Instance().SetupConstBuffer(bufferContent, ref Buffer.Value);
            Buffer.Value.DebugName = nameof(TransformsConstBuffer);
        }

        [StructLayout(LayoutKind.Explicit, Size = 4*4*4*10)]
        public struct BufferLayout
        {
            public BufferLayout(Matrix cameraToClipSpace, Matrix worldToCamera, Matrix objectToWorld)
            {
                Matrix clipSpaceToCamera = cameraToClipSpace;
                clipSpaceToCamera.Invert();
                Matrix cameraToWorld = worldToCamera;
                cameraToWorld.Invert();
                Matrix worldToObject = objectToWorld;
                worldToObject.Invert();
                
                CameraToClipSpace = cameraToClipSpace;
                ClipSpaceToCamera = clipSpaceToCamera;
                WorldToCamera = worldToCamera;
                CameraToWorld = cameraToWorld;
                WorldToClipSpace = Matrix.Multiply(worldToCamera, cameraToClipSpace);
                ClipSpaceToWorld = Matrix.Multiply(clipSpaceToCamera, cameraToWorld);
                ObjectToWorld = objectToWorld;
                WorldToObject = worldToObject;
                ObjectToCamera = Matrix.Multiply(objectToWorld, worldToCamera);
                ObjectToClipSpace = Matrix.Multiply(ObjectToCamera, cameraToClipSpace);

                // transpose all as mem layout in hlsl constant buffer is row based
                CameraToClipSpace.Transpose();
                ClipSpaceToCamera.Transpose();
                WorldToCamera.Transpose();
                CameraToWorld.Transpose();
                WorldToClipSpace.Transpose();
                ClipSpaceToWorld.Transpose();
                ObjectToWorld.Transpose();
                WorldToObject.Transpose();
                ObjectToCamera.Transpose();
                ObjectToClipSpace.Transpose();
            }

            [FieldOffset(0)]
            public Matrix CameraToClipSpace;
            [FieldOffset(64)]
            public Matrix ClipSpaceToCamera;
            [FieldOffset(128)]
            public Matrix WorldToCamera;
            [FieldOffset(192)]
            public Matrix CameraToWorld;
            [FieldOffset(256)]
            public Matrix WorldToClipSpace;
            [FieldOffset(320)]
            public Matrix ClipSpaceToWorld;
            [FieldOffset(384)]
            public Matrix ObjectToWorld;
            [FieldOffset(448)]
            public Matrix WorldToObject;
            [FieldOffset(512)]
            public Matrix ObjectToCamera;
            [FieldOffset(576)]
            public Matrix ObjectToClipSpace;
        }
    }
}