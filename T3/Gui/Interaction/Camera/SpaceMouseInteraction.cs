using SharpDX;
using T3;
using T3.Core;

namespace t3.Gui.Interaction.Camera
{
    public static class SpaceMouseInteraction
    {
        private static Vector3 _spaceMouseTranslateVector;
        private static Vector3 _spaceMouseRotateVector;
        private static int _eventCount;
        
        public static void ManipulateCamera(CameraSetup intendedSetup)
        {
            if (!_initialized)
            {
                Program.SpaceMouse.MotionEvent += SpaceMouseMotionHandler;
                Program.SpaceMouse.ButtonEvent += SpaceMouseButtonHandler;
                _initialized = true;
            }
            
            if (_eventCount == 0)
                return;

            var viewDir = intendedSetup.Target.ToSharpDxVector3() - intendedSetup.Position.ToSharpDxVector3();
            var upDir = new Vector3(0, 1, 0);
            var sideDir = Vector3.Cross(upDir, viewDir);
            
            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            float translationVelocity = _spaceMouseTranslateVector.Length() / 2000.0f;
            var direction = _spaceMouseTranslateVector;
            direction.Normalize();

            if (translationVelocity <  CameraInteractionParameters.MaxMoveVelocity)
                direction *= translationVelocity;
            else
                direction *= CameraInteractionParameters.MaxMoveVelocity;

            var moveDir = direction.X * sideDir - direction.Y * viewDir - direction.Z * upDir;

            var rotAroundX = Matrix.RotationAxis(sideDir, -_spaceMouseRotateVector.X / 8000.0f);
            var rotAroundY = Matrix.RotationAxis(upDir, -_spaceMouseRotateVector.Y / 8000.0f);
            var rot = Matrix.Multiply(rotAroundX, rotAroundY);
            var newViewDir = Vector3.Transform(viewDir, rot);
            newViewDir.Normalize();

            var oldPosition = intendedSetup.Position;
            intendedSetup.Position += moveDir.ToNumerics();
            
            intendedSetup.Target = oldPosition + (moveDir + newViewDir.ToVector3() * viewDirLength).ToNumerics();;
            //Log.Debug($"space mouse move: {moveDir}  eventCount:{_eventCount}");

            _eventCount = 0;
            _spaceMouseTranslateVector = new Vector3(0, 0, 0);
            _spaceMouseRotateVector = new Vector3(0, 0, 0);
        }
        
        private static void SpaceMouseButtonHandler(object sender, SpaceMouse.ButtonEventArgs e)
        {
            //_cameraInteraction.MoveVelocity = new SharpDX.Vector3(0, 0, 0);
            //_renderConfig.CameraSetup.ResetCamera();
            //App.Current.UpdateRequiredAfterUserInteraction = true;
        }

        private static void SpaceMouseMotionHandler(object sender, SpaceMouse.MotionEventArgs e)
        {
            if (e.TranslationVector != null)
            {
                _spaceMouseTranslateVector.X -= e.TranslationVector.X;
                _spaceMouseTranslateVector.Y += e.TranslationVector.Y;
                _spaceMouseTranslateVector.Z += e.TranslationVector.Z;
            }
            if (e.RotationVector != null)
            {
                // Swap axes from HID orientation to matching coordinates
                _spaceMouseRotateVector.X += e.RotationVector.X * 0.4f;
                _spaceMouseRotateVector.Y += e.RotationVector.Z;
                _spaceMouseRotateVector.Z += e.RotationVector.Y;
            }
            _eventCount++;
        }

        private static bool _initialized = false;
    }
}