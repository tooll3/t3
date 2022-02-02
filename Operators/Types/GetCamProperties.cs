using System;
using System.Numerics;
using SharpDX;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6;

namespace T3.Operators.Types.Id_843c9378_6836_4f39_b676_06fd2828af3e
{
    public class GetCamProperties : Instance<GetCamProperties>
    {
        // [Output(Guid = "ef803f31-907d-4aad-9f74-37226a0f64d4")]
        // public readonly Slot<Vector3> Position = new Slot<Vector3>();
        //
        // [Output(Guid = "53cd9f5b-9a36-4508-b42d-393e83f96c5e")]
        // public readonly Slot<Vector3> Direction = new Slot<Vector3>();

        [Output(Guid = "740ec7ee-2859-4dd1-97c2-781b5cbff352")]
        public readonly Slot<SharpDX.Vector4[]> WorldToCamera = new Slot<SharpDX.Vector4[]>();

        [Output(Guid = "7FB6FE30-5D12-4522-9E41-7BAFBB8E5CCE")]
        public readonly Slot<SharpDX.Vector4[]> CameraToClipSpace = new Slot<SharpDX.Vector4[]>();


        public GetCamProperties()
        {
            // Position.UpdateAction = Update;
            // Direction.UpdateAction = Update;
            // AspectRatio.UpdateAction = Update;
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
                Log.Warning("Camera reference is undefined");
                return;
            }

            if (obj is Camera camera)
            {
                Log.Debug("found camera " + camera);
            }
            else
            {
                Log.Warning("invalid type");
                return;
            }

            
            Matrix clipSpaceToCamera = camera.CameraToClipSpace;
            clipSpaceToCamera.Invert();
            Matrix cameraToWorld = camera.WorldToCamera2;
            cameraToWorld.Invert();
            // Matrix worldToObject = objectToWorld;
            // worldToObject.Invert();
                
            var CameraToClipSpace = camera.CameraToClipSpace;
            var ClipSpaceToCamera = clipSpaceToCamera;
            var WorldToCamera = camera.WorldToCamera2;
            var CameraToWorld = cameraToWorld;
            var WorldToClipSpace = Matrix.Multiply(WorldToCamera, CameraToClipSpace);
            var ClipSpaceToWorld = Matrix.Multiply(clipSpaceToCamera, cameraToWorld);
            // var ObjectToWorld = objectToWorld;
            // var WorldToObject = worldToObject;
            // var ObjectToCamera = Matrix.Multiply(objectToWorld, worldToCamera);
            // var ObjectToClipSpace = Matrix.Multiply(ObjectToCamera, cameraToClipSpace);

            // transpose all as mem layout in hlsl constant buffer is row based
            CameraToClipSpace.Transpose();
            ClipSpaceToCamera.Transpose();
            WorldToCamera.Transpose();
            CameraToWorld.Transpose();
            WorldToClipSpace.Transpose();
            ClipSpaceToWorld.Transpose();
            
            // WorldToCamera.Value[0] = camera.WorldToCamera2.Row1;
            // WorldToCamera.Value[1] = camera.WorldToCamera2.Row2;
            // WorldToCamera.Value[2] = camera.WorldToCamera2.Row3;
            // WorldToCamera.Value[3] = camera.WorldToCamera2.Row4;
            //
            // CameraToClipSpace.Value[0] = camera.CameraToClipSpace.Row1;
            // CameraToClipSpace.Value[1] = camera.CameraToClipSpace.Row2;
            // CameraToClipSpace.Value[2] = camera.CameraToClipSpace.Row3;
            // CameraToClipSpace.Value[3] = camera.CameraToClipSpace.Row4;
            
            // SharpDX.Matrix camToWorld = context.WorldToCamera;
            // camToWorld.Invert();
            //
            // var pos = SharpDX.Vector4.Transform(new SharpDX.Vector4(0f, 0f, 0f, 1f), camToWorld);
            // Position.Value = new Vector3(pos.X, pos.Y, pos.Z);
            //
            // var dir = SharpDX.Vector4.Transform(new SharpDX.Vector4(0f, 0f, 1f, 1f), camToWorld) - pos;
            // Direction.Value = new Vector3(dir.X, dir.Y, dir.Z);
            //
            // float aspect = context.CameraToClipSpace.M22 / context.CameraToClipSpace.M11;
            // AspectRatio.Value = aspect;
            //
            // Position.DirtyFlag.Clear();
            // Direction.DirtyFlag.Clear();
            // AspectRatio.DirtyFlag.Clear();
        }

        [Input(Guid = "A3190889-5473-4870-97CF-93E6CF94132B")]
        public readonly InputSlot<Object> CameraReference = new InputSlot<Object>();

    }
}

