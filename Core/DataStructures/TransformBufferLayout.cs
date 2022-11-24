using System.Runtime.InteropServices;
using SharpDX;
using T3.Core.Operator;

namespace T3.Core.DataStructures
{
        [StructLayout(LayoutKind.Explicit, Size = StructSize)]
        public struct TransformBufferLayout
        {
            public TransformBufferLayout(EvaluationContext context)
            {
                var cameraToClipSpace = context.CameraToClipSpace;
                var worldToCamera = context.WorldToCamera;
                var objectToWorld = context.ObjectToWorld;
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
            [FieldOffset(64 * 1)]
            public Matrix ClipSpaceToCamera;
            [FieldOffset(64 * 2)]
            public Matrix WorldToCamera;
            [FieldOffset(64 * 3)]
            public Matrix CameraToWorld;
            [FieldOffset(64 * 4)]
            public Matrix WorldToClipSpace;
            [FieldOffset(64 * 5)]
            public Matrix ClipSpaceToWorld;
            [FieldOffset(64 * 6)]
            public Matrix ObjectToWorld;
            [FieldOffset(64 * 7)]
            public Matrix WorldToObject;
            [FieldOffset(64 * 8)]
            public Matrix ObjectToCamera;
            [FieldOffset(64 * 9)]
            public Matrix ObjectToClipSpace;
            
            private const int StructSize = 64 * 10;
        }
}