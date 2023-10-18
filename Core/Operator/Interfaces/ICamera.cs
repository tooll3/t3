using SharpDX;
using T3.Core.Utils;
using Vector3 = System.Numerics.Vector3;

namespace T3.Core.Operator.Interfaces
{
    public interface ICamera
    {
        Vector3 CameraPosition { get; set; }
        Vector3 CameraTarget { get; set; }
        float CameraRoll { get; set; }

        CameraDefinition CameraDefinition { get; }
        SharpDX.Matrix WorldToCamera { get; }
        SharpDX.Matrix CameraToClipSpace { get; }
    }

    public struct CameraDefinition
    {
        public System.Numerics.Vector2 NearFarClip;
        public System.Numerics.Vector2 ViewPortShift;
        public System.Numerics.Vector3 PositionOffset;
        public System.Numerics.Vector3 Position;
        public System.Numerics.Vector3 Target;
        public System.Numerics.Vector3 Up;
        public float AspectRatio;
        public float Fov;
        public float Roll;
        public System.Numerics.Vector3 RotationOffset;
        public bool OffsetAffectsTarget;

        public static CameraDefinition Blend(CameraDefinition a, CameraDefinition b, float f)
        {
            return new CameraDefinition
                       {
                           NearFarClip = MathUtils.Lerp(a.NearFarClip, b.NearFarClip, f),
                           ViewPortShift = MathUtils.Lerp(a.ViewPortShift, b.ViewPortShift, f),
                           PositionOffset = MathUtils.Lerp(a.PositionOffset, b.PositionOffset, f),
                           Position = MathUtils.Lerp(a.Position, b.Position, f),
                           Target = MathUtils.Lerp(a.Target, b.Target, f),
                           Up = MathUtils.Lerp(a.Up, b.Up, f),
                           AspectRatio = MathUtils.Lerp(a.AspectRatio, b.AspectRatio, f),
                           Fov = MathUtils.Lerp(a.Fov, b.Fov, f),
                           Roll = MathUtils.Lerp(a.Roll, b.Roll, f),
                           RotationOffset = MathUtils.Lerp(a.RotationOffset, b.RotationOffset, f),
                           OffsetAffectsTarget = f < 0.5 ? a.OffsetAffectsTarget : b.OffsetAffectsTarget,
                       };
        }

        public void BuildProjectionMatrices(out Matrix camToClipSpace, out Matrix worldToCamera)
        {
            camToClipSpace = Matrix.PerspectiveFovRH(Fov, AspectRatio, NearFarClip.X, NearFarClip.Y);
            camToClipSpace.M31 = ViewPortShift.X;
            camToClipSpace.M32 = ViewPortShift.Y;

            var eye = Position;
            if (!OffsetAffectsTarget)
                eye += PositionOffset;

            var worldToCameraRoot = Matrix.LookAtRH(eye.ToSharpDx(), Target.ToSharpDx(), Up.ToSharpDx());
            var rollRotation = Matrix.RotationAxis(new SharpDX.Vector3(0, 0, 1), -(float)Roll);
            var additionalTranslation = OffsetAffectsTarget ? Matrix.Translation(PositionOffset.X, PositionOffset.Y, PositionOffset.Z) : Matrix.Identity;

            var additionalRotation = Matrix.RotationYawPitchRoll(MathUtil.DegreesToRadians(RotationOffset.Y),
                                                                 MathUtil.DegreesToRadians(RotationOffset.X),
                                                                 MathUtil.DegreesToRadians(RotationOffset.Z));

            worldToCamera = worldToCameraRoot * rollRotation * additionalRotation * additionalTranslation;
        }
    }

    // Mock view internal fallback camera (if no operator selected)
    // Todo: Find a better location of this class
    public class ViewCamera : ICamera
    {
        public Vector3 CameraPosition { get; set; } = new Vector3(0, 0, 2.416f);
        public Vector3 CameraTarget { get; set; }
        public float CameraRoll { get; set; }
        public Matrix WorldToCamera { get; }
        public Matrix CameraToClipSpace { get; }
        public CameraDefinition CameraDefinition => new();  // Not implemetned
    }
}