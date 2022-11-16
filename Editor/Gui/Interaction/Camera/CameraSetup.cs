using System.Numerics;
using T3.Core.Operator.Interfaces;

namespace T3.Editor.Gui.Interaction.Camera
{
    /// <summary>
    /// Describes a complete setup of a camera position and rotation. This
    /// is used for blending between cameras or smoothing camera interactions. 
    /// </summary>
    /// <remarks>Setup is a slightly misleading name. PositionAndOrientation would
    /// clearer but is too long.</remarks>
    public class CameraSetup
    {
        public Vector3 Position = new Vector3(0, 0, CameraInteractionParameters.DefaultCameraPositionZ);
        public Vector3 Target;

        public void Reset()
        {
            Position = new Vector3(0, 0, CameraInteractionParameters.DefaultCameraPositionZ);
            Target = Vector3.Zero;
        }

        public bool MatchesSetup(CameraSetup other)
        {
            return Vector3.Distance(other.Position, Position) < CameraInteractionParameters.StopDistanceThreshold
                   && Vector3.Distance(other.Target, Target) < CameraInteractionParameters.StopDistanceThreshold;
        }

        public void SetTo(CameraSetup other)
        {
            Position = other.Position;
            Target = other.Target;
        }

        public void SetTo(ICamera cameraInstance)
        {
            Position = cameraInstance.CameraPosition;
            Target = cameraInstance.CameraTarget;
        }

        public bool Matches(ICamera cameraInstance)
        {
            return Vector3.Distance(cameraInstance.CameraPosition, Position) < CameraInteractionParameters.StopDistanceThreshold
                   && Vector3.Distance(cameraInstance.CameraTarget, Target) < CameraInteractionParameters.StopDistanceThreshold;
        }

        public void BlendTo(CameraSetup intended, float cameraMoveFriction, float deltaTime)
        {
            var f = deltaTime * cameraMoveFriction * 60;
            Position = Vector3.Lerp(Position, intended.Position, f);
            Target = Vector3.Lerp(Target, intended.Target, f);
        }
    }
}