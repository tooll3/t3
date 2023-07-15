using System.Numerics;
using System.Runtime.InteropServices;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_a60adc26_d7c6_4615_af78_8d2d6da46b79
{
    public class TransformsConstBuffer : Instance<TransformsConstBuffer>
    {
        [Output(Guid = "7A76D147-4B8E-48CF-AA3E-AAC3AA90E888", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        public TransformsConstBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            //Log.Debug($" ObjectToWorld: {context.ObjectToWorld}", this);
            var bufferContent = new TransformBufferLayout(context.CameraToClipSpace, context.WorldToCamera, context.ObjectToWorld);
            ResourceManager.SetupConstBuffer(bufferContent, ref Buffer.Value);
            Buffer.Value.DebugName = nameof(TransformsConstBuffer);
        }

        [StructLayout(LayoutKind.Explicit, Size = 4*4*4*10)]
        public struct TransformBufferLayout
        {
            public TransformBufferLayout(Matrix4x4 cameraToClipSpace, Matrix4x4 worldToCamera, Matrix4x4 objectToWorld)
            {
                Matrix4x4 clipSpaceToCamera = cameraToClipSpace;
                clipSpaceToCamera.Invert();
                Matrix4x4 cameraToWorld = worldToCamera;
                cameraToWorld.Invert();
                Matrix4x4 worldToObject = objectToWorld;
                worldToObject.Invert();
                
                CameraToClipSpace = cameraToClipSpace;
                ClipSpaceToCamera = clipSpaceToCamera;
                WorldToCamera = worldToCamera;
                CameraToWorld = cameraToWorld;
                WorldToClipSpace = Matrix4x4.Multiply(worldToCamera, cameraToClipSpace);
                ClipSpaceToWorld = Matrix4x4.Multiply(clipSpaceToCamera, cameraToWorld);
                ObjectToWorld = objectToWorld;
                WorldToObject = worldToObject;
                ObjectToCamera = Matrix4x4.Multiply(objectToWorld, worldToCamera);
                ObjectToClipSpace = Matrix4x4.Multiply(ObjectToCamera, cameraToClipSpace);

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
            public Matrix4x4 CameraToClipSpace;
            [FieldOffset(64)]
            public Matrix4x4 ClipSpaceToCamera;
            [FieldOffset(128)]
            public Matrix4x4 WorldToCamera;
            [FieldOffset(192)]
            public Matrix4x4 CameraToWorld;
            [FieldOffset(256)]
            public Matrix4x4 WorldToClipSpace;
            [FieldOffset(320)]
            public Matrix4x4 ClipSpaceToWorld;
            [FieldOffset(384)]
            public Matrix4x4 ObjectToWorld;
            [FieldOffset(448)]
            public Matrix4x4 WorldToObject;
            [FieldOffset(512)]
            public Matrix4x4 ObjectToCamera;
            [FieldOffset(576)]
            public Matrix4x4 ObjectToClipSpace;
        }
    }
}