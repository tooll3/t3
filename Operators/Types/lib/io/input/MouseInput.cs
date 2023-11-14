using System;
using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_eff2ffff_dc39_4b90_9b1c_3c0a9a0108c6
{
    public class MouseInput : Instance<MouseInput>
    {
        [Output(Guid = "CDC87CE1-FAB8-4B96-9137-9965E064BFA3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector2> Position = new();

        [Output(Guid = "78CAABCF-9C3B-4E50-9D80-BDCBABAEB003", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> IsLeftButtonDown = new();

        [Output(Guid = "BE90EED3-26BF-4DC3-9771-073C04D359BC", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<System.Numerics.Vector3> Position3d = new();
        
        public MouseInput()
        {
            Position.UpdateAction = Update;
            Position3d.UpdateAction = Update;
            IsLeftButtonDown.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var mode = OutputMode.GetEnumValue<OutputModes>(context);
            var scale = Scale.GetValue(context);
            var aspectRatio = (float)context.RequestedResolution.Width / context.RequestedResolution.Height;

            var lastPosition = Core.IO.MouseInput.LastPosition;
            
            switch (mode)
            {
                case OutputModes.Normalized:
                    Position.Value = lastPosition;
                    Position3d.Value = new Vector3(lastPosition.X, lastPosition.Y, 0).ToNumerics();
                    break;
                case OutputModes.SignedPosition:
                    Position.Value = (lastPosition - new Vector2(0.5f, 0.5f)) * new Vector2(aspectRatio, -1) * scale;
                    Position3d.Value = new Vector3(lastPosition.X, lastPosition.Y, 0).ToNumerics();
                    break;
                case OutputModes.OnWorldXYPlane:
                case OutputModes.OnWorldFloorPlane:
                {
                    
                    var clipSpaceToWorld = ComposeClipSpaceToWorld(context);
                    var cameraToWorld = context.WorldToCamera;
                    cameraToWorld.Invert();
                    
                    var posInClip =  (lastPosition - new Vector2(0.5f, 0.5f)) * new Vector2(2, -2);
                    var posInWorld = Vector3.TransformCoordinate(new Vector3(posInClip.X, posInClip.Y, 0f), clipSpaceToWorld);
                    var targetInWorld = Vector3.TransformCoordinate(new Vector3(posInClip.X, posInClip.Y, 1f), clipSpaceToWorld);
                    var ray = new SharpDX.Ray(posInWorld, targetInWorld - posInWorld);

                    var xyPlaneName = mode == OutputModes.OnWorldXYPlane ? SharpDX.Vector3.UnitZ : SharpDX.Vector3.UnitY;
                    var xyPlane = new Plane(SharpDX.Vector3.Zero, xyPlaneName);
                    if (xyPlane.Intersects(ref ray, out SharpDX.Vector3 p))
                    {
                        Position.Value = new Vector2(p.X, p.Y) ;
                        Position3d.Value = p.ToNumerics();
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }

            IsLeftButtonDown.Value = Core.IO.MouseInput.IsLeftButtonDown;
            Position3d.DirtyFlag.Clear();
            Position.DirtyFlag.Clear();
            IsLeftButtonDown.DirtyFlag.Clear();
        }

        private static Matrix ComposeClipSpaceToWorld(EvaluationContext context)
        {
            var clipSpaceToCamera = context.CameraToClipSpace;
            clipSpaceToCamera.Invert();
            var cameraToWorld = context.WorldToCamera;
            cameraToWorld.Invert();
            var worldToObject = context.ObjectToWorld;
            worldToObject.Invert();
            var clipSpaceToWorld = Matrix.Multiply(clipSpaceToCamera, cameraToWorld);
            return clipSpaceToWorld;
        }

        [Input(Guid = "49775CC2-35B7-4C9F-A502-59FE8FBBE2A7")]
        public readonly InputSlot<bool> DoUpdate = new();

        [Input(Guid = "1327525C-716C-43E4-A5D1-58CF35440462", MappedType = typeof(OutputModes))]
        public readonly InputSlot<int> OutputMode = new();

        [Input(Guid = "6B1E81BD-2430-4439-AD0C-859B2433C38B")]
        public readonly InputSlot<float> Scale = new();

        private enum OutputModes
        {
            Normalized,
            SignedPosition,
            OnWorldXYPlane,
            OnWorldFloorPlane
        }
    }
}