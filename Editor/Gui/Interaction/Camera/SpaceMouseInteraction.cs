using System;
using SharpDX;
using Editor;
using T3.Core;
using Editor.Gui.UiHelpers;
using T3.Core.Resource;
using T3.Core.Utils;
using T3.Editor;
using T3.Editor.Gui.Interaction.Camera;

namespace Editor.Gui.Interaction.Camera
{
    /// <summary>
    /// Gathers update event information from SpaceMouse and apply it to a <see cref="CameraSetup"/>. 
    /// </summary>
    public static class SpaceMouseInteraction
    {
        public static void ManipulateCamera(CameraSetup intendedSetup, double frameTime)
        {
            if (!_initialized)
            {
                Program.SpaceMouse.MotionEvent += SpaceMouseMotionHandler;
                Program.SpaceMouse.ButtonEvent += SpaceMouseButtonHandler;
                _initialized = true;
            }
            
            var wasEvaluatedThisFrame = Math.Abs(frameTime - _lastUpdateTime) < 0.0001f;
            var stillMoving = _dampedRotation.LengthSquared() + _dampedTranslation.LengthSquared() > 0.001f;
            
            if (_eventCount == 0 && !stillMoving)
                return;

            if (!wasEvaluatedThisFrame)
            {
                var tooLong = Math.Abs(frameTime - _lastUpdateTime) > 0.5f;
                if (!tooLong)
                {
                    _dampedRotation= Vector3.Lerp( _rotationSum,_dampedRotation, UserSettings.Config.SpaceMouseDamping);
                    _dampedTranslation= Vector3.Lerp( _translationSum, _dampedTranslation,UserSettings.Config.SpaceMouseDamping);
                }
                else
                {
                    _dampedRotation = Vector3.Zero;
                    _dampedTranslation= Vector3.Zero;
                }
                _lastUpdateTime = frameTime;
            }
            _eventCount = 0;
            _translationSum = new Vector3(0, 0, 0);
            _rotationSum = new Vector3(0, 0, 0);

            
            var viewDir = intendedSetup.Target.ToSharpDxVector3() - intendedSetup.Position.ToSharpDxVector3();
            
            
            
            var upDir = new Vector3(0, 1, 0);
            var sideDir = Vector3.Cross(upDir, viewDir);
            
            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;
            
            // Prevent gimbal lock
            if (viewDir.Y > 0.95f)
            {
                _dampedRotation.X = MathF.Min(_dampedRotation.X, 0);
            }
            else if (viewDir.Y < -0.95f)
            {
                _dampedRotation.X = MathF.Max(_dampedRotation.X, 0);
            }

            float translationVelocity = _dampedTranslation.Length() / 2000.0f;
            var direction = _dampedTranslation;
            direction.Normalize();

            if (translationVelocity <  CameraInteractionParameters.MaxMoveVelocity)
                direction *= translationVelocity;
            else
                direction *= CameraInteractionParameters.MaxMoveVelocity;

            var moveDir = direction.X * sideDir - direction.Y * viewDir - direction.Z * upDir;

            var rotAroundX = Matrix.RotationAxis(sideDir, -_dampedRotation.X / 8000.0f);
            var rotAroundY = Matrix.RotationAxis(upDir, -_dampedRotation.Y / 8000.0f);
            var rot = Matrix.Multiply(rotAroundX, rotAroundY);
            var newViewDir = Vector3.Transform(viewDir, rot);
            newViewDir.Normalize();

            var oldPosition = intendedSetup.Position;
            intendedSetup.Position += moveDir.ToNumerics();
            
            intendedSetup.Target = oldPosition + (moveDir + newViewDir.ToVector3() * viewDirLength).ToNumerics();
            //Log.Debug($"space mouse move: {moveDir}  eventCount:{_eventCount}");

        }
        
        private static void SpaceMouseButtonHandler(object sender, SpaceMouse.ButtonEventArgs e)
        {
            // Todo: Think about what the buttons can be used for
        }

        private static void SpaceMouseMotionHandler(object sender, SpaceMouse.MotionEventArgs e)
        {
            if (e.TranslationVector != null)
            {
                _translationSum.X -= e.TranslationVector.X * UserSettings.Config.SpaceMouseMoveSpeedFactor;
                _translationSum.Y += e.TranslationVector.Y * UserSettings.Config.SpaceMouseMoveSpeedFactor;
                _translationSum.Z += e.TranslationVector.Z * UserSettings.Config.SpaceMouseMoveSpeedFactor;
            }
            if (e.RotationVector != null)
            {
                // Swap axes from HID orientation to matching coordinates
                _rotationSum.X += e.RotationVector.X * UserSettings.Config.SpaceMouseRotationSpeedFactor * 0.5f;
                _rotationSum.Y += e.RotationVector.Z * UserSettings.Config.SpaceMouseRotationSpeedFactor;
                _rotationSum.Z += e.RotationVector.Y * UserSettings.Config.SpaceMouseRotationSpeedFactor;
            }
            _eventCount++;
        }
        
        private static int _eventCount;
        private static Vector3 _translationSum;
        private static Vector3 _rotationSum;

        private static Vector3 _dampedTranslation;
        private static Vector3 _dampedRotation;

        private static double _lastUpdateTime;
        private static bool _initialized;
    }
}