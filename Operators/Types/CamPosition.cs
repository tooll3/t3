using System;
using System.Numerics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2ed26fb7_fe66_4ed6_8b8d_230d87ae5c77
{
    public class CamPosition : Instance<CamPosition>
    {
        [Output(Guid = "20E33049-C2FA-4C9F-8607-318B279B72EC", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector3> Position = new Slot<Vector3>();
        
        [Output(Guid = "5A38F00B-342A-46E5-9410-FBF403F2313E", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector3> Direction = new Slot<Vector3>();

        [Output(Guid = "A2213E94-0FE5-4CA6-A13D-9A265D50E707", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> AspectRatio = new Slot<float>();

        
        public CamPosition()
        {
            Position.UpdateAction = Update;
            Direction.UpdateAction = Update;
            AspectRatio.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            SharpDX.Matrix camToWorld = context.WorldToCamera;
            camToWorld.Invert();
            
            var pos = SharpDX.Vector4.Transform(new SharpDX.Vector4(0f, 0f, 0f, 1f), camToWorld);
            Position.Value = new Vector3(pos.X, pos.Y, pos.Z);
            
            var dir = SharpDX.Vector4.Transform(new SharpDX.Vector4(0f, 0f, 1f, 1f), camToWorld) - pos;
            Direction.Value = new Vector3(dir.X, dir.Y, dir.Z);
            
            float aspect = context.CameraToClipSpace.M22 / context.CameraToClipSpace.M11;
            AspectRatio.Value = aspect;
            
            Position.DirtyFlag.Clear();
            Direction.DirtyFlag.Clear();
            AspectRatio.DirtyFlag.Clear();
        }

        [Input(Guid = "eb265bf8-7ec5-4089-88ce-d8054d338ea7")]
        public readonly InputSlot<string> Variable = new InputSlot<string>();
    }
}

