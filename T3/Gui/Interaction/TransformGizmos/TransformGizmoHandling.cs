using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpDX;
using SharpDX.Mathematics.Interop;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Gui.Commands;
using t3.Gui.Commands.Graph;
using T3.Gui.UiHelpers;
using T3.Gui.Windows;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
// ReSharper disable RedundantNameQualifier

namespace T3.Gui.Interaction.TransformGizmos
{
    /**
     * Handles the interaction with 3d-gizmos for operators selected in the graph.  
     */
    static class TransformGizmoHandling
    {
        public static bool IsDragging => _draggedGizmoPart != GizmoParts.None;

        public static void RegisterSelectedTransformable(SymbolChildUi node, ITransformable transformable)
        {
            if (_selectedTransformables.Contains(transformable))
                return;

            transformable.TransformCallback = TransformCallback;
            _selectedTransformables.Add(transformable);
        }

        public static void ClearDeselectedTransformableNode(ITransformable transformable)
        {
            if (_selectedTransformables.Contains(transformable))
            {
                Log.Warning("trying to deselect an unregistered transformable?");
                return;
            }

            transformable.TransformCallback = null;
            _selectedTransformables.Remove(transformable);
        }

        public static void ClearSelectedTransformables()
        {
            foreach (var selectedTransformable in _selectedTransformables)
            {
                selectedTransformable.TransformCallback = null;
            }

            _selectedTransformables.Clear();
        }

        /// <summary>
        /// We need the foreground draw list at the moment when the output texture is drawn to the to output view...
        /// </summary>
        public static void SetDrawList(ImDrawListPtr drawList)
        {
            _drawList = drawList;
            _isDrawListValid = true;
        }

        public static void RestoreDrawList()
        {
            _isDrawListValid = false;
        }

        private static Vector3 _selectedCenter;

        public static Vector3 GetLatestSelectionCenter()
        {
            if (_selectedTransformables.Count == 0)
                return Vector3.Zero;

            return _selectedCenter;
        }

        /// <summary>
        /// Called from <see cref="ITransformable"/> operators during update call
        /// </summary>
        public static void TransformCallback(Instance instance, EvaluationContext context)
        {
            if (!_isDrawListValid)
            {
                Log.Warning("can't draw gizmo without initialized draw list");
                return;
            }

            if (instance is not ITransformable tmp)
                return;

            _instance = instance;

            _transformable = tmp;

            if (!_selectedTransformables.Contains(_transformable))
            {
                Log.Warning("transform-callback from non-selected node?" + _transformable);
                return;
            }

            if (context.ShowGizmos == GizmoVisibility.Off)
            {
                return;
            }

            _objectToWorld = context.ObjectToWorld;
            _objectToClipSpace = context.ObjectToWorld * context.WorldToCamera * context.CameraToClipSpace;

            UpdateInternalState();

            var gizmoScale = CalcGizmoScale(context, _localToObject, _viewport.Width, _viewport.Height, 45f, UserSettings.Config.GizmoSize);
            _centerPadding = 0.2f * gizmoScale / _canvas.Scale.X;
            _gizmoLength = 2f * gizmoScale / _canvas.Scale.Y;
            _planeGizmoSize = 0.5f * gizmoScale / _canvas.Scale.X;
            //var lineThickness = 2;

            var isHoveringSomething = HandleDragOnAxis(SharpDX.Vector3.UnitX, Color.Red, GizmoParts.PositionXAxis);
            isHoveringSomething |= HandleDragOnAxis(SharpDX.Vector3.UnitY, Color.Green, GizmoParts.PositionYAxis);
            isHoveringSomething |= HandleDragOnAxis(SharpDX.Vector3.UnitZ, Color.Blue, GizmoParts.PositionZAxis);
            isHoveringSomething |= HandleDragOnPlane(SharpDX.Vector3.UnitX, SharpDX.Vector3.UnitY, Color.Blue, GizmoParts.PositionOnXyPlane);
            isHoveringSomething |= HandleDragOnPlane(SharpDX.Vector3.UnitX, SharpDX.Vector3.UnitZ, Color.Green, GizmoParts.PositionOnXzPlane);
            isHoveringSomething |= HandleDragOnPlane(SharpDX.Vector3.UnitY, SharpDX.Vector3.UnitZ, Color.Red, GizmoParts.PositionOnYzPlane);
            isHoveringSomething |= HandleDragInScreenSpace();
        }

        static void UpdateInternalState()
        {
            // Terminology of the matrices:
            // objectToClipSpace means in this context the transform without application of the ITransformable values. These are
            // named 'local'. So localToObject is the matrix of applying the ITransformable values and localToClipSpace to transform
            // points from the local system (including trans/rot of ITransformable) to the projected space. Scale is ignored for
            // local here as the local values are only used for drawing and therefore we don't want to draw anything scaled by this values.
            _mousePosInScreen = ImGui.GetIO().MousePos;

            //var s = TryGetVectorFromInput(_transformable.ScaleInput, 1);
            var r = TryGetVectorFromInput(_transformable.RotationInput);
            var t = TryGetVectorFromInput(_transformable.TranslationInput);

            var yaw = SharpDX.MathUtil.DegreesToRadians(r.Y);
            var pitch = SharpDX.MathUtil.DegreesToRadians(r.X);
            var roll = SharpDX.MathUtil.DegreesToRadians(r.Z);

            var c = SharpDX.Vector3.TransformNormal(new SharpDX.Vector3(t.X, t.Y, t.Z), _objectToWorld);
            _selectedCenter = new Vector3(c.X, c.Y, c.Z);

            _localToObject = SharpDX.Matrix.Transformation(scalingCenter: SharpDX.Vector3.Zero,
                                                               scalingRotation: SharpDX.Quaternion.Identity,
                                                               scaling: SharpDX.Vector3.One,
                                                               rotationCenter: SharpDX.Vector3.Zero,
                                                               rotation: SharpDX.Quaternion.RotationYawPitchRoll(yaw, pitch, roll),
                                                               translation: new SharpDX.Vector3(t.X, t.Y, t.Z));
            _localToClipSpace = _localToObject * _objectToClipSpace;

            SharpDX.Vector4 originInClipSpace = SharpDX.Vector4.Transform(new SharpDX.Vector4(t.X, t.Y, t.Z, 1), _objectToClipSpace);

            // Don't draw gizmo behind camera (view plane)
            _originInClipSpace = new Vector3(originInClipSpace.X, originInClipSpace.Y, originInClipSpace.Z) / originInClipSpace.W;
            _renderGizmo = Math.Abs(_originInClipSpace.Z) <= 1 && Math.Abs(_originInClipSpace.X) <= 2 && Math.Abs(_originInClipSpace.Y) <= 2;

            var viewports = ResourceManager.Device.ImmediateContext.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();
            _viewport = viewports[0];
            var originInViewport = new Vector2(_viewport.Width * (_originInClipSpace.X * 0.5f + 0.5f),
                                               _viewport.Height * (1.0f - (_originInClipSpace.Y * 0.5f + 0.5f)));

            _canvas = ImageOutputCanvas.Current;
            var originInCanvas = _canvas.TransformDirection(originInViewport);
            _topLeftOnScreen = _canvas.TransformPosition(System.Numerics.Vector2.Zero);
            _originInScreen = _topLeftOnScreen + originInCanvas;

            _initialObjectToLocal = _localToObject;
            _initialObjectToLocal.Invert();
        }

        // Returns true if hovered or active
        static bool HandleDragOnAxis(SharpDX.Vector3 gizmoAxis, Color color, GizmoParts mode)
        {
            var axisStartInScreen = LocalPosToScreenPos(gizmoAxis * _centerPadding);
            var axisEndInScreen = LocalPosToScreenPos(gizmoAxis * _gizmoLength);

            var isHovering = false;
            if (!IsDragging)
            {
                isHovering = IsPointOnLine(_mousePosInScreen, axisStartInScreen, axisEndInScreen);

                if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    StartPositionDragging(mode, true);
                }
            }
            else if (_draggedGizmoPart == mode
                     && _draggedTransformable == _transformable
                     && _dragInteractionWindowId == ImGui.GetID(""))
            {
                isHovering = true;

                var rayInObject = GetPickRayInObject(_mousePosInScreen);
                var rayInLocal = rayInObject;
                rayInLocal.Direction = SharpDX.Vector3.TransformNormal(rayInObject.Direction, _initialObjectToLocal);
                rayInLocal.Position = SharpDX.Vector3.TransformCoordinate(rayInObject.Position, _initialObjectToLocal);

                SharpDX.Vector3 newOrigin = _initialOrigin; // (GetNewOffsetInObject() - _initialOrigin) * (gizmoAxis1 + gizmoAxis2) + _initialOrigin;
                if (_plane.Normal != SharpDX.Vector3.Zero
                    && _plane.Intersects(ref rayInLocal, out SharpDX.Vector3 intersectionPoint))
                {
                    SharpDX.Vector3 offsetInLocal = (intersectionPoint - _initialIntersectionPoint) * gizmoAxis;
                    var offsetInObject = SharpDX.Vector4.Transform(new SharpDX.Vector4(offsetInLocal, 1), _localToObject);
                    newOrigin = new SharpDX.Vector3(offsetInObject.X, offsetInObject.Y, offsetInObject.Z) / offsetInObject.W;
                }
                UpdatePositionDragging(newOrigin);

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    CompletePositionDragging();
                }
            }

            if (_renderGizmo)
                _drawList.AddLine(axisStartInScreen, axisEndInScreen, color, 2 * (isHovering ? 3 : 1));

            return isHovering;
        }

        // Returns true if hovered or active
        static bool HandleDragOnPlane(SharpDX.Vector3 gizmoAxis1, SharpDX.Vector3 gizmoAxis2, Color color, GizmoParts mode)
        {
            var origin = (gizmoAxis1 + gizmoAxis2) * _centerPadding;
            Vector2[] pointsOnScreen =
                {
                    LocalPosToScreenPos(origin),
                    LocalPosToScreenPos(origin + gizmoAxis1 * _planeGizmoSize),
                    LocalPosToScreenPos(origin + (gizmoAxis1 + gizmoAxis2) * _planeGizmoSize),
                    LocalPosToScreenPos(origin + gizmoAxis2 * _planeGizmoSize),
                };
            var isHovering = false;

            if (!IsDragging)
            {
                isHovering = IsPointInQuad(_mousePosInScreen, pointsOnScreen);

                if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    StartPositionDragging(mode, true);
                }
            }
            else if (_draggedGizmoPart == mode
                     && _draggedTransformable == _transformable
                     && _dragInteractionWindowId == ImGui.GetID(""))
            {
                isHovering = true;

                var rayInObject = GetPickRayInObject(_mousePosInScreen);
                var rayInLocal = rayInObject;
                rayInLocal.Direction = SharpDX.Vector3.TransformNormal(rayInObject.Direction, _initialObjectToLocal);
                rayInLocal.Position = SharpDX.Vector3.TransformCoordinate(rayInObject.Position, _initialObjectToLocal);

                SharpDX.Vector3 newOrigin = _initialOrigin; // (GetNewOffsetInObject() - _initialOrigin) * (gizmoAxis1 + gizmoAxis2) + _initialOrigin;
                if (_plane.Normal != SharpDX.Vector3.Zero
                    && _plane.Intersects(ref rayInLocal, out SharpDX.Vector3 intersectionPoint))
                {
                    SharpDX.Vector3 offsetInLocal = (intersectionPoint - _initialIntersectionPoint) * (gizmoAxis1 + gizmoAxis2);
                    var offsetInObject = SharpDX.Vector4.Transform(new SharpDX.Vector4(offsetInLocal, 1), _localToObject);
                    newOrigin = new SharpDX.Vector3(offsetInObject.X, offsetInObject.Y, offsetInObject.Z) / offsetInObject.W;
                }
                UpdatePositionDragging(newOrigin);

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    CompletePositionDragging();
                }
            }

            if (_renderGizmo)
            {
                var color2 = color;
                color2.Rgba.W = isHovering ? 0.4f : 0.2f;
                _drawList.AddConvexPolyFilled(ref pointsOnScreen[0], 4, color2);
            }
            return isHovering;
        }

        private static bool HandleDragInScreenSpace()
        {
            const float gizmoSize = 4;
            var screenSquaredMin = _originInScreen - new Vector2(gizmoSize, gizmoSize);
            var screenSquaredMax = _originInScreen + new Vector2(gizmoSize, gizmoSize);

            var isHovering = false;

            if (_draggedGizmoPart == GizmoParts.None)
            {
                isHovering = (_mousePosInScreen.X > screenSquaredMin.X && _mousePosInScreen.X < screenSquaredMax.X &&
                              _mousePosInScreen.Y > screenSquaredMin.Y && _mousePosInScreen.Y < screenSquaredMax.Y);
                if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    StartPositionDragging(GizmoParts.PositionInScreenPlane, true);
                }
            }
            else if (_draggedGizmoPart == GizmoParts.PositionInScreenPlane
                     && _draggedTransformable == _transformable
                     && _dragInteractionWindowId == ImGui.GetID(""))
            {
                isHovering = true;
                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    CompletePositionDragging();
                }
                else
                {
                    UpdatePositionDragging(GetNewOffsetInObject());
                    // not internal plane udpate needed here since it's not used at all
                }
            }

            if (_renderGizmo)
            {
                var color2 = Color.Orange;
                color2.Rgba.W = isHovering ? 0.8f : 0.3f;
                _drawList.AddRectFilled(screenSquaredMin, screenSquaredMax, color2);
            }

            return isHovering;
        }

        private static void StartPositionDragging(GizmoParts mode, bool createUndo)
        {
            _draggedGizmoPart = mode;
            if (createUndo)
            {
                _inputValueCommandInFlight = new ChangeInputValueCommand(_instance.Parent.Symbol,
                                                                         _instance.SymbolChildId,
                                                                         _transformable.TranslationInput.Input,
                                                                         _transformable.TranslationInput.Input.Value);
            }

            _draggedTransformable = _transformable;
            _dragInteractionWindowId = ImGui.GetID("");
            _initialOffsetToOrigin = _mousePosInScreen - _originInScreen;
            _initialOrigin = _localToObject.TranslationVector;

            var rayInObject = GetPickRayInObject(_mousePosInScreen);
            var rayInLocal = rayInObject;
            rayInLocal.Direction = SharpDX.Vector3.TransformNormal(rayInObject.Direction, _initialObjectToLocal);
            rayInLocal.Position = SharpDX.Vector3.TransformCoordinate(rayInObject.Position, _initialObjectToLocal);

            _plane = GetPlaneForDragMode(mode, rayInObject.Direction, _initialOrigin);
            if (mode == GizmoParts.PositionInScreenPlane)
            {
                _plane.Normal = SharpDX.Vector3.Zero;
            }
            else if (!_plane.Intersects(ref rayInLocal, out _initialIntersectionPoint))
            {
                _plane.D = -_plane.D;
                if (!_plane.Intersects(ref rayInLocal, out _initialIntersectionPoint))
                {
                    _plane.Normal = SharpDX.Vector3.Zero;
                    Log.Debug($"Couldn't intersect pick ray with gizmo axis plane.");
                }
            }
        }

        private static void UpdatePositionDragging(SharpDX.Vector3 newOrigin)
        {
            TrySetVector3ToInput(_transformable.TranslationInput, new Vector3(newOrigin.X, newOrigin.Y, newOrigin.Z));
            InputValue value = _transformable.TranslationInput.Input.Value;

            _inputValueCommandInFlight.AssignNewValue(value);
        }

        private static void CompletePositionDragging()
        {
            UndoRedoStack.Add(_inputValueCommandInFlight);
            _inputValueCommandInFlight = null;

            _draggedGizmoPart = GizmoParts.None;
            _draggedTransformable = null;
            _dragInteractionWindowId = 0;
        }

        private static SharpDX.Vector3 GetNewOffsetInObject()
        {
            Vector2 newOriginInScreen = _mousePosInScreen - _initialOffsetToOrigin;
            // transform back to object space
            var clipSpaceToObject = _objectToClipSpace;
            clipSpaceToObject.Invert();
            var newOriginInCanvas = newOriginInScreen - _topLeftOnScreen;
            var newOriginInViewport = _canvas.InverseTransformDirection(newOriginInCanvas);
            var newOriginInClipSpace = new SharpDX.Vector4(2.0f * newOriginInViewport.X / _viewport.Width - 1.0f,
                                                           -(2.0f * newOriginInViewport.Y / _viewport.Height - 1.0f),
                                                           _originInClipSpace.Z, 1);
            var newOriginInObject = SharpDX.Vector4.Transform(newOriginInClipSpace, clipSpaceToObject);
            Vector3 newTranslation = new Vector3(newOriginInObject.X, newOriginInObject.Y, newOriginInObject.Z) / newOriginInObject.W;
            return new SharpDX.Vector3(newTranslation.X, newTranslation.Y, newTranslation.Z);
        }

        private static Vector3 TryGetVectorFromInput(IInputSlot input, float defaultValue = 0f)
        {
            return input switch
                       {
                           InputSlot<Vector3> vec3Input => vec3Input.Value,
                           InputSlot<Vector2> vec2Input => new Vector3(vec2Input.Value.X, vec2Input.Value.Y, defaultValue),
                           _                            => new Vector3(defaultValue, defaultValue, defaultValue)
                       };
        }

        private static void TrySetVector3ToInput(IInputSlot input, Vector3 vector3)
        {
            switch (input)
            {
                case InputSlot<Vector3> vec3Input:
                    vec3Input.SetTypedInputValue(vector3);
                    break;
                case InputSlot<Vector2> vec2Input:
                    vec2Input.SetTypedInputValue(new Vector2(vector3.X, vector3.Y));
                    break;
            }
        }

        #region math
        private static Ray GetPickRayInObject(Vector2 posInScreen)
        {
            var clipSpaceToObject = _objectToClipSpace;
            clipSpaceToObject.Invert();
            var newOriginInCanvas = posInScreen - _topLeftOnScreen;
            var newOriginInViewport = _canvas.InverseTransformDirection(newOriginInCanvas);

            float xInClipSpace = 2.0f * newOriginInViewport.X / _viewport.Width - 1.0f;
            float yInClipSpace = -(2.0f * newOriginInViewport.Y / _viewport.Height - 1.0f);

            var rayStartInClipSpace = new SharpDX.Vector3(xInClipSpace, yInClipSpace, 0);
            var rayStartInObject = SharpDX.Vector3.TransformCoordinate(rayStartInClipSpace, clipSpaceToObject);

            var rayEndInClipSpace = new SharpDX.Vector3(xInClipSpace, yInClipSpace, 1);
            var rayEndInObject = SharpDX.Vector3.TransformCoordinate(rayEndInClipSpace, clipSpaceToObject);

            var rayDir = (rayEndInObject - rayStartInObject);
            rayDir.Normalize();

            return new SharpDX.Ray(rayStartInObject, rayDir);
        }

        // Calculates the scale for a gizmo based on the distance to the cam
        private static float CalcGizmoScale(EvaluationContext context, SharpDX.Matrix localToObject, float width, float height, float fovInDegree,
                                            float gizmoSize)
        {
            var localToCamera = localToObject * context.ObjectToWorld * context.WorldToCamera;
            var distance = localToCamera.TranslationVector.Length(); // distance of local origin to cam
            var denom = Math.Sqrt(width * width + height * height) * Math.Tan(SharpDX.MathUtil.DegreesToRadians(fovInDegree));
            return (float)Math.Max(0.0001, (distance / denom) * gizmoSize);
        }

        private static Vector2 LocalPosToScreenPos(SharpDX.Vector3 posInLocal)
        {
            var homogenousPosInLocal = new SharpDX.Vector4(posInLocal.X, posInLocal.Y, posInLocal.Z, 1);
            SharpDX.Vector4 originInClipSpace = SharpDX.Vector4.Transform(homogenousPosInLocal, _localToClipSpace);
            Vector3 posInNdc = new Vector3(originInClipSpace.X, originInClipSpace.Y, originInClipSpace.Z) / originInClipSpace.W;
            var viewports = ResourceManager.Device.ImmediateContext.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();
            var viewport = viewports[0];
            var originInViewport = new Vector2(viewport.Width * (posInNdc.X * 0.5f + 0.5f),
                                               viewport.Height * (1.0f - (posInNdc.Y * 0.5f + 0.5f)));

            var posInCanvas = _canvas.TransformDirection(originInViewport);
            return _topLeftOnScreen + posInCanvas;
        }

        private static SharpDX.Plane GetPlaneForDragMode(GizmoParts mode, SharpDX.Vector3 normDir, SharpDX.Vector3 origin)
        {
            switch (mode)
            {
                case GizmoParts.PositionXAxis:
                {
                    var secondAxis = Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitY)) <
                                     Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitZ))
                                         ? SharpDX.Vector3.UnitY
                                         : SharpDX.Vector3.UnitZ;
                    return new Plane(origin, origin + SharpDX.Vector3.UnitX, origin + secondAxis);
                }
                case GizmoParts.PositionYAxis:
                {
                    var secondAxis = Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitX)) <
                                     Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitZ))
                                         ? SharpDX.Vector3.UnitX
                                         : SharpDX.Vector3.UnitZ;
                    return new Plane(origin, origin + SharpDX.Vector3.UnitY, origin + secondAxis);
                }
                case GizmoParts.PositionZAxis:
                {
                    var secondAxis = Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitX)) <
                                     Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitY))
                                         ? SharpDX.Vector3.UnitX
                                         : SharpDX.Vector3.UnitY;
                    return new Plane(origin, origin + SharpDX.Vector3.UnitZ, origin + secondAxis);
                }
                case GizmoParts.PositionOnXyPlane:
                    return new Plane(origin, origin + SharpDX.Vector3.UnitX, origin + SharpDX.Vector3.UnitY);

                case GizmoParts.PositionOnXzPlane:
                    return new Plane(origin, origin + SharpDX.Vector3.UnitX, origin + SharpDX.Vector3.UnitZ);

                case GizmoParts.PositionOnYzPlane:
                    return new Plane(origin, origin + SharpDX.Vector3.UnitY, origin + SharpDX.Vector3.UnitZ);
            }

            Log.Error($"GetIntersectionPlane(...) called with wrong GizmoDraggingMode.");
            return new Plane(origin, origin + SharpDX.Vector3.UnitX, origin + SharpDX.Vector3.UnitY);
        }

        private static bool IsPointOnLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float threshold = 3)
        {
            var rect = new ImRect(lineStart, lineEnd).MakePositive();
            rect.Expand(threshold);
            if (!rect.Contains(point))
                return false;

            var positionOnLine = GetClosestPointOnLine(point, lineStart, lineEnd);
            return Vector2.Distance(point, positionOnLine) <= threshold;
        }

        private static Vector2 GetClosestPointOnLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            var v = (lineEnd - lineStart);
            var vLen = v.Length();

            var d = Vector2.Dot(v, point - lineStart) / vLen;
            return lineStart + v * d / vLen;
        }

        private static bool IsPointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var a = 0.5f * (-p1.Y * p2.X + p0.Y * (-p1.X + p2.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y);
            var sign = a < 0 ? -1 : 1;
            var s = (p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y) * sign;
            var t = (p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y) * sign;

            return s > 0 && t > 0 && (s + t) < 2 * a * sign;
        }

        private static bool IsPointInQuad(Vector2 p, Vector2[] corners)
        {
            return IsPointInTriangle(p, corners[0], corners[1], corners[2])
                   || IsPointInTriangle(p, corners[0], corners[2], corners[3]);
        }
        #endregion

        public enum GizmoParts
        {
            None,
            PositionInScreenPlane,
            PositionXAxis,
            PositionYAxis,
            PositionZAxis,
            PositionOnXyPlane,
            PositionOnXzPlane,
            PositionOnYzPlane,
        }

        private static ImDrawListPtr _drawList = null;
        private static bool _isDrawListValid;

        private static uint _dragInteractionWindowId;

        private static readonly HashSet<ITransformable> _selectedTransformables = new();
        private static Instance _instance;
        private static ITransformable _transformable;

        private static GizmoParts _draggedGizmoPart = GizmoParts.None;
        private static ITransformable _draggedTransformable;
        private static ChangeInputValueCommand _inputValueCommandInFlight;

        private static float _centerPadding;
        private static float _gizmoLength;
        private static float _planeGizmoSize;

        private static RawViewportF _viewport;
        private static Vector2 _mousePosInScreen;
        private static ImageOutputCanvas _canvas;
        private static Vector2 _topLeftOnScreen;

        private static Vector2 _originInScreen;
        private static Vector3 _originInClipSpace;
        private static bool _renderGizmo;

        // Keep values when interaction started
        private static SharpDX.Vector3 _initialOrigin;
        private static Vector2 _initialOffsetToOrigin;
        private static Matrix _initialObjectToLocal;
        private static SharpDX.Vector3 _initialIntersectionPoint;

        private static Plane _plane;

        private static Matrix _objectToClipSpace;
        private static Matrix _objectToWorld;
        private static Matrix _localToObject;
        private static Matrix _localToClipSpace;
    }
}