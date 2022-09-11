using System;
using System.Runtime.InteropServices;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_e6f2a00d_854e_412e_94a1_a21df91fc988
{
    public class ShadowMapTransformsConstBuffer : Instance<ShadowMapTransformsConstBuffer>
    {
        [Output(Guid = "9b622d38-52e8-4a6d-8c03-4fc6eb8051b0", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Buffer> Buffer = new Slot<Buffer>();

        [Input(Guid = "94d229a4-8bbf-4be4-a2f1-9b52cc785490")]
        public readonly InputSlot<System.Numerics.Vector3> Position = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "14038263-253a-4025-a7e5-fd6ca7dfb949")]
        public readonly InputSlot<System.Numerics.Vector3> Target = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "5783c091-816e-43a7-94ba-2e0df6380b0d")]
        public readonly InputSlot<System.Numerics.Vector2> NearFarClip = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "82c12036-6f0e-4da6-913c-0a2c37919466")]
        public readonly InputSlot<System.Numerics.Vector2> Size = new InputSlot<System.Numerics.Vector2>();

        public ShadowMapTransformsConstBuffer()
        {
            Buffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            System.Numerics.Vector2 size = Size.GetValue(context);
            System.Numerics.Vector2 clip = NearFarClip.GetValue(context);
            Matrix cameraToClipSpace = Matrix.OrthoRH(size.X, size.Y, clip.X, clip.Y);
            
            var pos = Position.GetValue(context);
            Vector3 eye = new Vector3(pos.X, pos.Y, pos.Z);
            var t = Target.GetValue(context);
            Vector3 target = new Vector3(t.X, t.Y, t.Z);
            Vector3 viewDir = target - eye;
            viewDir.Normalize();
            Vector3 upRef = (Math.Abs(Vector3.Dot(viewDir, Vector3.Up)) > 0.9) ? Vector3.Left : Vector3.Up;
            Vector3 up = Vector3.Cross(upRef, target - eye);
            Matrix worldToCamera = Matrix.LookAtRH(eye, target, up);

            var bufferContent = new BufferLayout(cameraToClipSpace, worldToCamera, context.ObjectToWorld);
            ResourceManager.Instance().SetupConstBuffer(bufferContent, ref Buffer.Value);
            Buffer.Value.DebugName = nameof(ShadowMapTransformsConstBuffer);
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
            public Matrix WorldToObject;
            [FieldOffset(448)]
            public Matrix ObjectToWorld;
            [FieldOffset(512)]
            public Matrix ObjectToCamera;
            [FieldOffset(576)]
            public Matrix ObjectToClipSpace;
        }
    }
}