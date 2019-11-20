using System;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Operators.Types;

namespace T3.Gui.Graph.Interaction
{
    public class CameraInteraction
    {
        public void UpdateCameraNode(Camera cameraNode)
        {
            _cameraNode = cameraNode;
        }
        
        public void Update()
        {
            if (_cameraNode == null)
                return;
            
            ImGui.Text("Camera:" + _cameraNode);

            _viewAxis.ComputeForCamera(_cameraNode);
            _deltaTime = ImGui.GetIO().DeltaTime;

            var cameraSwitched = _cameraNode != _lastCameraNode;
            if (cameraSwitched)
            {
                _intendedSetup.SetTo(_cameraNode);
                _smoothedSetup.SetTo(_cameraNode);
                _moveVelocity = Vector3.Zero;
                _lastCameraNode = _cameraNode;
            }

            ManipulateCameraByMouse();
            ManipulateCameraByKeyboard();
            ComputeSmoothMovement();
            
            _cameraNode.Position.Input.IsDefault = false;
            _cameraNode.Position.TypedInputValue.Value = _smoothedSetup.Position;
            _cameraNode.Position.DirtyFlag.Invalidate();
            _cameraNode.Target.Input.IsDefault = false;
            _cameraNode.Target.TypedInputValue.Value = _smoothedSetup.Target;
            _cameraNode.Target.DirtyFlag.Invalidate();

            // TODO: Camera-Update doesn't work
            _cameraNode.Position.Value = _intendedSetup.Position;
            _cameraNode.Target.Value = _intendedSetup.Target;
            // Log.Debug($"pos{_smoothedSetup.Position}  target:{_smoothedSetup.Target}  vel: {_moveVelocity}");
        }
        

        private void ComputeSmoothMovement()
        {
            var cameraIsStillMoving = (_smoothedSetup.MatchesSetup(_intendedSetup)
                                       || _moveVelocity.Length() > StopDistanceThreshold
                                       || _lookingAroundDelta.Length() > StopDistanceThreshold
                                       || _orbitVelocity.Length() > 0.001f
                                       || _manipulatedByMouseWheel
                                       || _manipulatedByKeyboard);
            if (!cameraIsStillMoving)
            {
                _smoothedSetup.SetTo(_intendedSetup);
                _orbitVelocity = Vector2.Zero;
                _moveVelocity= Vector3.Zero;
                return;
            }

            if (_orbitVelocity.Length() > 0.001f)
            {
                OrbitByAngle(_orbitVelocity);
                _orbitVelocity = Vector2.Lerp(_orbitVelocity, Vector2.Zero, _deltaTime * OrbitHorizontalFriction * 60);;
            }

            if (_moveVelocity.Length() > MaxMoveVelocity)
            {
                _moveVelocity *= MaxMoveVelocity / _moveVelocity.Length();
            }
            else if(!_manipulatedByKeyboard)
            {
                _moveVelocity = Vector3.Lerp(_moveVelocity, Vector3.Zero, _deltaTime * CameraMoveFriction * 60);    
            }
            
            _intendedSetup.Position += _moveVelocity * _deltaTime;
            _intendedSetup.Target += (_moveVelocity + _lookingAroundDelta) * _deltaTime;
            
            _smoothedSetup.BlendTo(_intendedSetup, CameraMoveFriction);
            _lookingAroundDelta = Vector3.Zero;

            _manipulatedByMouseWheel = false;
            _manipulatedByKeyboard = false;
        }

        private void HandleMouseWheel()
        {
            var delta = ImGui.GetIO().MouseWheel;
            var viewDirection = _smoothedSetup.MatchesSetup(_intendedSetup)
                                    ? _intendedSetup.Position - _intendedSetup.Target
                                    : _smoothedSetup.Position - _smoothedSetup.Target;

            var zoomFactorForCurrentFramerate = 1 + (ZoomSpeed * FrameDurationFactor);

            if (delta < 0)
            {
                viewDirection *= zoomFactorForCurrentFramerate;
            }
            else
            {
                viewDirection /= zoomFactorForCurrentFramerate;
            }

            _smoothedSetup.Position = _intendedSetup.Position = _intendedSetup.Target + viewDirection;
            _manipulatedByMouseWheel = true;
        }

        private void ManipulateCameraByMouse()
        {
            HandleMouseWheel();
            if (ImGui.IsMouseDragging(0))
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
                    DragOrbit();
                }
            }
            else if (ImGui.IsMouseClicked(1))
            {
                Pan();
            }
            else if (ImGui.IsMouseClicked(2))
            {
                LookAround();
            }
        }


        private void LookAround()
        {
            var dragDelta = ImGui.GetMouseDragDelta();
            var factorX = (float)(dragDelta.X / RenderWindowHeight * RotateMouseSensitivity * Math.PI / 180.0);
            var factorY = (float)(dragDelta.Y / RenderWindowHeight * RotateMouseSensitivity * Math.PI / 180.0);
            var rotAroundX = Matrix4x4.CreateFromAxisAngle(_viewAxis.Left, factorY);
            var rotAroundY = Matrix4x4.CreateFromAxisAngle(_viewAxis.Up, factorX);
            var rot = Matrix4x4.Multiply(rotAroundX, rotAroundY);

            var viewDir2 = new Vector4(_intendedSetup.Target - _intendedSetup.Position, 1);
            var viewDirRotated = Vector4.Transform(viewDir2, rot);

            var newTarget = _intendedSetup.Target + new Vector3(viewDirRotated.X, viewDirRotated.Y, viewDirRotated.Z);
            _lookingAroundDelta = newTarget - _intendedSetup.Target;
        }

        private void DragOrbit()
        {
            //var dragDelta = ImGui.GetMouseDragDelta();
            var dragDelta = ImGui.GetIO().MouseDelta;
            _orbitVelocity += dragDelta * _deltaTime * -0.2f;
            // new Vector2((float)(dragDelta.X / RenderWindowHeight * OrbitSensitivity * Math.PI / 180.0),
            //                           (float)(dragDelta.Y / RenderWindowHeight * OrbitSensitivity * Math.PI / 180.0));
        }

        private void OrbitByAngle(Vector2 orbitVelocity)
        {
            Log.Debug("Orbit by angle " + orbitVelocity);
            var currentTarget = _intendedSetup.Target;
            var viewDir = new Vector4(_intendedSetup.Target - _intendedSetup.Position, 1);
            var viewDirLength = viewDir.Length();
            viewDir /= viewDirLength;

            var rotAroundX = Matrix4x4.CreateFromAxisAngle(_viewAxis.Left, orbitVelocity.Y);
            var rotAroundY = Matrix4x4.CreateFromAxisAngle(_viewAxis.Up, orbitVelocity.X);
            var rot = Matrix4x4.Multiply(rotAroundX, rotAroundY);

            var newViewDir = Vector4.Transform(viewDir, rot);
            newViewDir = Vector4.Normalize(newViewDir);

            // Set new position and freeze cam-target transitions
            _intendedSetup.Position = _smoothedSetup.Position =
                                          _intendedSetup.Target - new Vector3(newViewDir.X, newViewDir.Y, newViewDir.Z) * viewDirLength;
            _intendedSetup.Target = currentTarget;
        }

        private void Pan()
        {
            var dragDelta = ImGui.GetMouseDragDelta();
            var factorX = -dragDelta.X / RenderWindowHeight;
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
            
            if (ImGui.IsKeyPressed((int)Key.A) || ImGui.IsKeyPressed((int)Key.CursorLeft))
            {
                _moveVelocity += _viewAxis.Left * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyPressed((int)Key.D) || ImGui.IsKeyPressed((int)Key.CursorRight))
            {
                _moveVelocity -= _viewAxis.Left * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyPressed((int)Key.W) || ImGui.IsKeyPressed((int)Key.CursorUp))
            {
                _moveVelocity += Vector3.Normalize(_viewAxis.ViewDistance) * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyPressed((int)Key.S) || ImGui.IsKeyPressed((int)Key.CursorDown))
            {
                _moveVelocity -= Vector3.Normalize(_viewAxis.ViewDistance) * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyPressed((int)Key.E))
            {
                _moveVelocity += _viewAxis.Up * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyPressed((int)Key.X))
            {
                _moveVelocity -= _viewAxis.Up * acc;
                _manipulatedByKeyboard = true;
            }

            if (ImGui.IsKeyPressed((int)Key.F))
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

            public void ComputeForCamera(Camera camera)
            {
                ViewDistance = camera.Target.Value - camera.Position.Value;

                var worldUp = Vector3.UnitY;
                var rolledUp = Vector3.Normalize(Vector3.Transform(worldUp, Matrix4x4.CreateFromAxisAngle(ViewDistance, camera.Roll.Value)));

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
                       && Vector3.Distance(other.Target, Target) > StopDistanceThreshold;
            }

            public void SetTo(CameraSetup other)
            {
                Position = other.Position;
                Target = other.Target;
            }

            public void SetTo(Camera cameraInstance)
            {
                Position = cameraInstance.Position.Value;
                Target = cameraInstance.Target.Value;
            }

            public void BlendTo(CameraSetup intended, float cameraMoveFriction)
            {
                var f = _deltaTime * cameraMoveFriction;
                Position = Vector3.Lerp(intended.Position, Position, f);
                Target = Vector3.Lerp(intended.Target, Target, f);
            }

            private const float DefaultCameraPositionZ = -10;
        }

        private static ViewAxis _viewAxis = new ViewAxis();
        private readonly CameraSetup _smoothedSetup = new CameraSetup();
        private readonly CameraSetup _intendedSetup = new CameraSetup();
        
        private static float FrameDurationFactor => (ImGui.GetIO().DeltaTime);
        private bool _manipulatedByMouseWheel;
        private bool _manipulatedByKeyboard;
        
        private Vector3 _moveVelocity;
        private Vector3 _lookingAroundDelta = Vector3.Zero;
        private Vector2 _orbitVelocity;
        private Camera _cameraNode;
        private Camera _lastCameraNode;
        private static float _deltaTime;

        private const float RenderWindowHeight = 450; // TODO: this should be derived from output window size
        private const float StopDistanceThreshold = 0.001f;
        private const float RotateMouseSensitivity = 300;
        private const float OrbitHorizontalFriction = 0.2f;
        private const float CameraMoveFriction = 0.03f;
        private const float ZoomSpeed = 0.2f;

        private const float MaxMoveVelocity = 200;
        private const float CameraAcceleration = 10;
    }
}