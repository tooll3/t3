using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Operators.Types.Id_2ed26fb7_fe66_4ed6_8b8d_230d87ae5c77
{
    public class CamPosition : Instance<CamPosition>
    {
        
        [Output(Guid = "51BEC9E0-2E6E-49B6-885C-2AA0F3AC37E3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Command> Command = new();
        
        [Output(Guid = "20E33049-C2FA-4C9F-8607-318B279B72EC", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector3> Position = new();
        
        [Output(Guid = "5A38F00B-342A-46E5-9410-FBF403F2313E", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector3> Direction = new();

        [Output(Guid = "A2213E94-0FE5-4CA6-A13D-9A265D50E707", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> AspectRatio = new();

        
        public CamPosition()
        {
            Command.UpdateAction = Update;
            Position.UpdateAction = Update;
            Direction.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Matrix4x4.Invert(context.WorldToCamera, out var camToWorld);
            
            var pos = Vector4.Transform(new Vector4(0f, 0f, 0f, 1f), camToWorld);
            Position.Value = new Vector3(pos.X, pos.Y, pos.Z);
            
            var dir = pos -Vector4.Transform(new Vector4(0f, 0f, 1f, 1f), camToWorld);
            Direction.Value = new Vector3(dir.X, dir.Y, dir.Z);
            
            float aspect = context.CameraToClipSpace.M22 / context.CameraToClipSpace.M11;
            AspectRatio.Value = aspect;
            
            Position.DirtyFlag.Clear();
            Direction.DirtyFlag.Clear();
            AspectRatio.DirtyFlag.Clear();
        }
    }
}

