using System;
using System.Collections.Generic;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_746d886c_5ab6_44b1_bb15_f3ce2fadf7e6;
using T3.Operators.Types.Id_843c9378_6836_4f39_b676_06fd2828af3e;
using Vector3 = System.Numerics.Vector3;

namespace T3.Operators.Types.Id_5b538cf5_e3b6_4674_b23e_ab55fc59ada6
{
    public class GetCamProperties2 : Instance<GetCamProperties2>
    {
        [Output(Guid = "6776D95E-F046-457E-944C-578CDA9E98AF")]
        public readonly Slot<Vector3> Rotation = new ();
        
        
        [Output(Guid = "013B08CB-AF63-4FAC-BA28-DE5D1F5A869C")]
        public readonly Slot<Vector3> Position = new ();
        
        
        [Output(Guid = "0FDF4500-9582-49A5-B383-6ECAE14D8DD5")]
        public readonly Slot<int> Count = new ();
        
        public GetCamProperties2()
        {
            Count.UpdateAction = Update;
            Position.UpdateAction = Update;
        }

        private List<ICameraPropertiesProvider> _cameraInstances = new();
        
        private void Update(EvaluationContext context)
        {
            try
            {
                _cameraInstances.Clear();
                foreach (var child in Parent.Parent.Children)
                {
                    if (child is not ICameraPropertiesProvider camera)
                        continue;

                    _cameraInstances.Add(camera);
                }
            }
            catch (Exception e)
            {
                Log.Warning("Failed to access cameras: " + e.Message);
                return;
            }

            Count.Value = _cameraInstances.Count;
            Log.Debug("Count: " + _cameraInstances.Count);
            
            var index = CameraIndex.GetValue(context).Clamp(0,10000);
            
            if (_cameraInstances.Count == 0)
            {
                Log.Debug("No cameras found");
                return;
            }

            var cam= _cameraInstances[ index % _cameraInstances.Count];

            if (cam is Camera camInstance)
            {
                Log.Debug($"Get camera {camInstance.SymbolChildId} index: {index}");
            }
            
            var camToWorld = cam.WorldToCamera;
            camToWorld.Invert();
            
            var pos = new Vector3(camToWorld.M41, camToWorld.M42, camToWorld.M43);
            Position.Value = pos;

            SharpDX.Vector3 scale, translation;
            Quaternion rotation;
            camToWorld.Decompose(out scale, out rotation, out translation);
            
            // var position = new Vector3(matrix.M41, 
            //                            matrix.M42, 
            //                            matrix.M43);
            Position.Value = new Vector3(translation.X,
                                         translation.Y,
                                         translation.Z);
            //var euler = rotation.
            //Rotation.Value = -rotationMatrixToEulerAngles(matrix) / MathF.PI * 180;
            camToWorld.Invert();
            camToWorld.Transpose();
            

            
            var forward1 = SharpDX.Vector4.Transform(new SharpDX.Vector4(0, 0, 0.1f,1), camToWorld);
            var forward2 = SharpDX.Vector4.Transform(new SharpDX.Vector4(0, 0, 0,1), camToWorld);
            var forward = forward1 - forward2;

            Rotation.Value = new Vector3(0,
                                         -MathF.Atan2(forward.X, forward.Y) * 180 / MathF.PI + 180,
                                         0);
            
            Rotation.DirtyFlag.Clear();
            Position.DirtyFlag.Clear();

            //Rotation.Value = ToEulerAngles(rotation) / MathF.PI * 180 ;


        }
        
        private Vector3 rotationMatrixToEulerAngles(Matrix R)
        {

            //assert(isRotationMatrix(R));

            float sy = MathF.Sqrt((R.M11 * R.M11 +  R.M21 * R.M21 ));

            bool singular = sy < 1e-6; // If

            float x, y, z;
            if (!singular)
            {
                x = MathF.Atan2(R.M32 , R.M33);
                y = MathF.Atan2(-R.M31, sy);
                z = MathF.Atan2(R.M21, R.M11);
            }
            else
            {
                x = MathF.Atan2(-R.M23, R.M22);
                y = MathF.Atan2(-R.M31, sy);
                z = 0;
            }
            return new Vector3(x, y, z);
        }
        
        public static Vector3 ToEulerAngles(Quaternion q)
        {
            //EulerAngles angles = new();
            var angles = new Vector3();

            // roll (x-axis rotation)
            var sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            var cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            var rx = MathF.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            var sinp = 2 * (q.W * q.Y - q.Z * q.X);
            
            var ry = MathF.Abs(sinp) >= 1 ? (int)MathF.CopySign(MathF.PI / 2, sinp) : (int)MathF.Asin(sinp);

            // yaw (z-axis rotation)
            var siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            var cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            var rz = MathF.Atan2(siny_cosp, cosy_cosp);

            return new Vector3(rx, ry, rz);
        }        
        
        

        [Input(Guid = "F7D2B9BC-4D01-4E3B-91ED-4E41FF387196")]
        public readonly InputSlot<int> CameraIndex = new();
        
    }
}

