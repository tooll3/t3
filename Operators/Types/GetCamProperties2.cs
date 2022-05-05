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
        [Output(Guid = "013B08CB-AF63-4FAC-BA28-DE5D1F5A869C")]
        public readonly Slot<Vector3> Position = new();

        [Output(Guid = "F9A31409-323C-43C8-B850-624050EA229E")]
        public readonly Slot<SharpDX.Vector4[]> MatrixRows = new();
        
        [Output(Guid = "0FDF4500-9582-49A5-B383-6ECAE14D8DD5")]
        public readonly Slot<int> Count = new();

        public GetCamProperties2()
        {
            Count.UpdateAction = Update;
            Position.UpdateAction = Update;
            MatrixRows.UpdateAction = Update;
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

            var index = CameraIndex.GetValue(context).Clamp(0, 10000);

            if (_cameraInstances.Count == 0)
            {
                Log.Debug("No cameras found");
                return;
            }

            var cam = _cameraInstances[index % _cameraInstances.Count];

            if (cam is not ICameraPropertiesProvider camInstance)
            {
                Log.Warning($"Camera #{index}/{_cameraInstances.Count} is not a Camera");
                return;
            }

            var camToWorld = cam.WorldToCamera;
            camToWorld.Invert();

            var pos = new Vector3(camToWorld.M41, camToWorld.M42, camToWorld.M43);
            Position.Value = pos;
            MatrixRows.Value = new[] { camToWorld.Row1, camToWorld.Row2, camToWorld.Row3, camToWorld.Row4, };
        }
        

        [Input(Guid = "F7D2B9BC-4D01-4E3B-91ED-4E41FF387196")]
        public readonly InputSlot<int> CameraIndex = new();
    }
}