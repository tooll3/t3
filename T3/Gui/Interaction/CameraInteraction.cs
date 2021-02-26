using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Core.Operator.Interfaces;

namespace T3.Gui.Graph.Interaction
{

    public class CameraInteraction
    {
        public void Update(ICamera camera, bool allowCameraInteraction)
        {
            if (camera == null)
                return;

            var cameraNodeModified = !_smoothedSetup.Matches(camera) && !_intendedSetup.Matches(camera);
            if (cameraNodeModified)
            {
                _intendedSetup.SetTo(camera);
                _smoothedSetup.SetTo(camera);
                return;
            }

            _viewAxis.ComputeForCamera(camera);
            _deltaTime = ImGui.GetIO().DeltaTime;

            var cameraSwitched = camera != _lastCameraNode;
            if (cameraSwitched)
            {
                _intendedSetup.SetTo(camera);
                _smoothedSetup.SetTo(camera);
                _moveVelocity = Vector3.Zero;
                _lastCameraNode = camera;
            }

            if (allowCameraInteraction && ImGui.IsWindowFocused() && ImGui.IsWindowHovered())
            {
                ManipulateCameraByMouse();
                ManipulateCameraByKeyboard();
            }

            var updateRequired = ComputeSmoothMovement();
            if (!updateRequired)
                return;

            camera.CameraPosition = _smoothedSetup.Position;
            camera.CameraTarget = _smoothedSetup.Target;
        }

        private bool ComputeSmoothMovement()
        {
            var stillDamping = !_smoothedSetup.MatchesSetup(_intendedSetup);
            var stillSliding = _moveVelocity.Length() > StopDistanceThreshold;
            var stillOrbiting = _orbitVelocity.Length() > 0.001f;
            
            var cameraIsStillMoving = stillDamping
                                      || stillSliding
                                      || stillOrbiting
                                      || _manipulatedByMouseWheel
                                      || _manipulatedByKeyboard;
            if (!cameraIsStillMoving)
            {
                _smoothedSetup.SetTo(_intendedSetup);
                _orbitVelocity = Vector2.Zero;
                _moveVelocity = Vector3.Zero;
                return false;
            }

            if (_orbitVelocity.Length() > 0.001f)
            {
                ApplyOrbitVelocity(_orbitVelocity);
                _orbitVelocity = Vector2.Lerp(_orbitVelocity, Vector2.Zero, _deltaTime * OrbitHorizontalDamping * 60);
            }

            var maxVelocityForScale = _viewAxis.ViewDistance.Length() * MaxMoveVelocity;
            if (_moveVelocity.Length() > maxVelocityForScale)
            {
                _moveVelocity *= maxVelocityForScale / _moveVelocity.Length();
            }

            if (!_manipulatedByKeyboard)
            {
                _moveVelocity = Vector3.Lerp(_moveVelocity, Vector3.Zero, _deltaTime * CameraMoveDamping * 60);
            }

            _intendedSetup.Position += _moveVelocity * _deltaTime;
            _intendedSetup.Target += _moveVelocity * _deltaTime;

            _smoothedSetup.BlendTo(_intendedSetup, CameraDamping);

            _manipulatedByMouseWheel = false;
            _manipulatedByKeyboard = false;
            return true;
        }

        private void ManipulateCameraByMouse()
        {
            HandleMouseWheel();
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (ImGui.GetIO().KeyAlt)
                {
                    Pan();
                }
                else if (ImGui.GetIO().KeyCtrl)
                {
                    LookAround();
                }
                else
                {
                    var delta = new Vector2(ImGui.GetIO().MouseDelta.X,
                                            -ImGui.GetIO().MouseDelta.Y);
                    _orbitVelocity += delta * _deltaTime * -0.1f;
                }
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                Pan();
            }
            else if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
            {
                LookAround();
            }
        }

        private void HandleMouseWheel()
        {
            var delta = ImGui.GetIO().MouseWheel;
            if (Math.Abs(delta) < 0.01f)
                return;

            var viewDistance = _intendedSetup.Position - _intendedSetup.Target;

            var zoomFactorForCurrentFramerate = 1 + (ZoomSpeed * FrameDurationFactor);

            if (delta < 0)
            {
                viewDistance *= zoomFactorForCurrentFramerate;
            }

            if (delta > 0)
            {
                viewDistance /= zoomFactorForCurrentFramerate;
            }

            _intendedSetup.Position = _intendedSetup.Target + viewDistance;
            _manipulatedByMouseWheel = true;
        }

        private void LookAround()
        {
            if (!ImGui.IsWindowFocused())
                return;

            var dragDelta = ImGui.GetIO().MouseDelta;
            var factorX = -dragDelta.X * RotateMouseSensitivity;
            var factorY = dragDelta.Y * RotateMouseSensitivity;
            var rotAroundX = Matrix4x4.CreateFromAxisAngle(_viewAxis.Left, factorY);
            var rotAroundY = Matrix4x4.CreateFromAxisAngle(_viewAxis.Up, factorX);
            var rot = Matrix4x4.Multiply(rotAroundX, rotAroundY);

            var viewDir2 = new Vector4(_intendedSetup.Target - _intendedSetup.Position, 1);
            var viewDirRotated = Vector4.Transform(viewDir2, rot);
            viewDirRotated = Vector4.Normalize(viewDirRotated);

            var newTarget = _intendedSetup.Position + new Vector3(viewDirRotated.X, viewDirRotated.Y, viewDirRotated.Z);
            _intendedSetup.Target = newTarget;
        }

        private void ApplyOrbitVelocity(Vector2 orbitVelocity)
        {
            var viewDir = new Vector4(_intendedSetup.Target - _intendedSetup.Position, 1);
            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            var rotAroundX = Matrix4x4.CreateFromAxisAngle(_viewAxis.Left, orbitVelocity.Y);
            var rotAroundY = Matrix4x4.CreateFromAxisAngle(_viewAxis.Up, orbitVelocity.X);
            var rot = Matrix4x4.Multiply(rotAroundX, rotAroundY);

            var newViewDir = Vector4.Transform(viewDir, rot);
            newViewDir = Vector4.Normalize(newViewDir);
            _intendedSetup.Position = _intendedSetup.Target - new Vector3(newViewDir.X, newViewDir.Y, newViewDir.Z) * viewDirLength;
        }

        private void Pan()
        {
            if (!ImGui.IsWindowFocused())
                return;

            var dragDelta = ImGui.GetIO().MouseDelta;
            var factorX = dragDelta.X / RenderWindowHeight;
            var factorY = dragDelta.Y / RenderWindowHeight;

            var length = (_intendedSetup.Target - _intendedSetup.Position).Length();
            var delta = _viewAxis.Left * factorX * length
                        + _viewAxis.Up * factorY * length;

            _intendedSetup.Position += delta;
            _intendedSetup.Target += delta;
        }

        private void ManipulateCameraByKeyboard()
        {
            if (!ImGui.IsWindowHovered())
                return;

            var viewDirLength = _viewAxis.ViewDistance.Length();
            var acc = CameraAcceleration * _deltaTime * 60 * viewDirLength;

            if (ImGui.IsKeyDown((int)Key.A) || ImGui.IsKeyDown((int)Key.CursorLeft))
            {
                _moveVelocity += _viewAxis.Left * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((int)Key.D) || ImGui.IsKeyDown((int)Key.CursorRight))
            {
                _moveVelocity -= _viewAxis.Left * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((int)Key.W) || ImGui.IsKeyDown((int)Key.CursorUp))
            {
                _moveVelocity += Vector3.Normalize(_viewAxis.ViewDistance) * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((int)Key.S) || ImGui.IsKeyDown((int)Key.CursorDown))
            {
                _moveVelocity -= Vector3.Normalize(_viewAxis.ViewDistance) * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((int)Key.E))
            {
                _moveVelocity += _viewAxis.Up * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((int)Key.X))
            {
                _moveVelocity -= _viewAxis.Up * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyDown((int)Key.F))
            {
                _moveVelocity = Vector3.Zero;
                _intendedSetup.Reset();
                _manipulatedByKeyboard = true;
            }
        }

        private struct ViewAxis
        {
            public Vector3 Up;
            public Vector3 Left;
            public Vector3 ViewDistance;

            public void ComputeForCamera(ICamera camera)
            {
                ViewDistance = camera.CameraTarget - camera.CameraPosition;

                var worldUp = Vector3.UnitY;
                var rolledUp = Vector3.Normalize(Vector3.Transform(worldUp, Matrix4x4.CreateFromAxisAngle(ViewDistance, camera.CameraRoll)));

                Left = Vector3.Normalize(Vector3.Cross(rolledUp, ViewDistance));
                Up = Vector3.Normalize(Vector3.Cross(ViewDistance, Left));
            }
        }

        private class CameraSetup
        {
            public Vector3 Position = new Vector3(0, 0, DefaultCameraPositionZ);
            public Vector3 Target;

            public void Reset()
            {
                Position = new Vector3(0, 0, DefaultCameraPositionZ);
                Target = Vector3.Zero;
            }

            public bool MatchesSetup(CameraSetup other)
            {
                return Vector3.Distance(other.Position, Position) < StopDistanceThreshold
                       && Vector3.Distance(other.Target, Target) < StopDistanceThreshold;
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
                return Vector3.Distance(cameraInstance.CameraPosition, Position) < StopDistanceThreshold
                       && Vector3.Distance(cameraInstance.CameraTarget, Target) < StopDistanceThreshold;
            }

            public void BlendTo(CameraSetup intended, float cameraMoveFriction)
            {
                var f = _deltaTime * cameraMoveFriction * 60;
                Position = Vector3.Lerp(Position, intended.Position, f);
                Target = Vector3.Lerp(Target, intended.Target, f);
            }

            private const float DefaultCameraPositionZ = 2.416f;
        }

        private static ViewAxis _viewAxis = new ViewAxis();
        private readonly CameraSetup _smoothedSetup = new CameraSetup();
        private readonly CameraSetup _intendedSetup = new CameraSetup();

        private static float FrameDurationFactor => (ImGui.GetIO().DeltaTime);
        private bool _manipulatedByMouseWheel;
        private bool _manipulatedByKeyboard;

        private Vector3 _moveVelocity;
        private Vector2 _orbitVelocity;
        private ICamera _lastCameraNode;
        private static float _deltaTime;

        private const float RenderWindowHeight = 450; // TODO: this should be derived from output window size
        private const float StopDistanceThreshold = 0.0001f;
        private const float RotateMouseSensitivity = 0.001f;
        private const float OrbitHorizontalDamping = 0.2f;
        private const float CameraMoveDamping = 0.12f;
        private const float CameraDamping = 0.5f;
        private const float ZoomSpeed = 10f;

        private const float MaxMoveVelocity = 1;
        private const float CameraAcceleration = 1;
    }
}