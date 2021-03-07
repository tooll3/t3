using System;
using System.Collections.Generic;
using System.Diagnostics;
using ImGuiNET;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Interfaces;
using T3.Gui.Windows;
using UiHelpers;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace T3.Gui.Selection
{
    static class TransformGizmoHandling
    {

        public static GizmoDraggingModes CurrentDraggingMode = GizmoDraggingModes.None;
        public static void RegisterSelectedTransformable(SymbolChildUi node, ITransformable transformable)
        {
            if (SelectedTransformables.Contains(transformable))
                return;
            
            SelectedTransformables.Add(transformable);
            transformable.TransformCallback = TransformCallback;
        }
        public static void ClearDeselectedTransformableNode(ITransformable transformable)
        {
            if (SelectedTransformables.Contains(transformable))
            {
                Log.Warning("trying to deselect an unregistered transformable?");
                return;
            }

            transformable.TransformCallback = null;
            SelectedTransformables.Remove(transformable);
        }

        public static void ClearSelectedTransformables()
        {
            foreach (var selectedTransformable in SelectedTransformables)
            {
                selectedTransformable.TransformCallback = null;
            }

            SelectedTransformables.Clear();
        }

        
        /// <summary>
        /// need foreground draw list atm as texture is drawn afterwards to output view
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
            if(SelectedTransformables.Count == 0)
                return Vector3.Zero;
            

            return _selectedCenter;
        }
        
        
        /// <summary>
        /// Called from <see cref="ITransformable"/> nodes during update
        /// </summary>
        public static void TransformCallback(ITransformable transformable, EvaluationContext context)
        {
            if (!_isDrawListValid)
            {
                Log.Warning("can't draw gizmo without initialized draw list");
                return;
            }

            if (!SelectedTransformables.Contains(transformable))
            {
                Log.Warning("transform-callback from non-selected node?" + transformable);
                return;
            }

            // terminology of the matrices:
            // objectToClipSpace means in this context the transform without application of the ITransformable values. These are
            // named 'local'. So localToObject is the matrix of applying the ITransformable values and localToClipSpace to transform
            // points from the local system (including trans/rot of ITransformable) to the projected space. Scale is ignored for
            // local here as the local values are only used for drawing and therefore we don't want to draw anything scaled by this values.
            var objectToClipSpace = context.ObjectToWorld * context.WorldToCamera * context.CameraToClipSpace;


            var s = transformable.Scale;
            var r = transformable.Rotation;
            float yaw = SharpDX.MathUtil.DegreesToRadians(r.Y);
            float pitch = SharpDX.MathUtil.DegreesToRadians(r.X);
            float roll = SharpDX.MathUtil.DegreesToRadians(r.Z);
            var t = transformable.Translation;
            
            var c=SharpDX.Vector3.TransformNormal(new SharpDX.Vector3(t.X,t.Y, t.Z), context.ObjectToWorld);
            _selectedCenter = new Vector3(c.X, c.Y, c.Z);

            var localToObject = SharpDX.Matrix.Transformation(SharpDX.Vector3.Zero, SharpDX.Quaternion.Identity, SharpDX.Vector3.One,
                                                              SharpDX.Vector3.Zero, SharpDX.Quaternion.RotationYawPitchRoll(yaw, pitch, roll),
                                                              new SharpDX.Vector3(t.X, t.Y, t.Z));
            var localToClipSpace = localToObject * objectToClipSpace;

            SharpDX.Vector4 originInClipSpace = SharpDX.Vector4.Transform(new SharpDX.Vector4(t.X, t.Y, t.Z, 1), objectToClipSpace);
            
            // Don't draw gizmo behind camera
            Vector3 originInNdc = new Vector3(originInClipSpace.X, originInClipSpace.Y, originInClipSpace.Z) / originInClipSpace.W;
            if ((originInNdc.Z > 1 || Math.Abs(originInNdc.X) > 2 
                                  || Math.Abs(originInNdc.Y) > 2) 
                && CurrentDraggingMode == GizmoDraggingModes.None)
                return;
            
            var viewports = ResourceManager.Instance().Device.ImmediateContext.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();
            var viewport = viewports[0];
            var originInViewport = new Vector2(viewport.Width * (originInNdc.X * 0.5f + 0.5f),
                                               viewport.Height * (1.0f - (originInNdc.Y * 0.5f + 0.5f)));

            var canvas = ImageOutputCanvas.Current;
            var originInCanvas = canvas.TransformDirection(originInViewport);
            var topLeftOnScreen = ImageOutputCanvas.Current.TransformPosition(System.Numerics.Vector2.Zero);
            var originInScreen = topLeftOnScreen + originInCanvas;


            var gizmoScale = CalcGizmoScale(context, localToObject, viewport.Width, viewport.Height, 45f, SettingsWindow.GizmoSize);
            var centerPadding = 0.2f * gizmoScale / canvas.Scale.X;
            var length = 2f * gizmoScale / canvas.Scale.Y;
            var planeGizmoSize = 0.5f * gizmoScale / canvas.Scale.X;
            var lineThickness = 2;

            var mousePosInScreen = ImGui.GetIO().MousePos;

            var isHoveringSomething = DoGizmoAxis(SharpDX.Vector3.UnitX, Color.Red, GizmoDraggingModes.PositionXAxis);
            isHoveringSomething |= DoGizmoAxis(SharpDX.Vector3.UnitY, Color.Green, GizmoDraggingModes.PositionYAxis);
            isHoveringSomething |= DoGizmoAxis(SharpDX.Vector3.UnitZ, Color.Blue, GizmoDraggingModes.PositionZAxis);

            if (!isHoveringSomething)
            {
                isHoveringSomething |= DoGizmoPlane(SharpDX.Vector3.UnitX, SharpDX.Vector3.UnitY, Color.Blue, GizmoDraggingModes.PositionOnXyPlane);
                isHoveringSomething |= DoGizmoPlane(SharpDX.Vector3.UnitX, SharpDX.Vector3.UnitZ, Color.Green, GizmoDraggingModes.PositionOnXzPlane);
                isHoveringSomething |= DoGizmoPlane(SharpDX.Vector3.UnitY, SharpDX.Vector3.UnitZ, Color.Red, GizmoDraggingModes.PositionOnYzPlane);
            }

            if (!isHoveringSomething)
            {
                HandleDragInScreenSpace();
            }

            // Returns true if hovered or active
            bool DoGizmoAxis(SharpDX.Vector3 gizmoAxis, Color color, GizmoDraggingModes mode)
            {
                var axisStartInScreen = ObjectPosToScreenPos(gizmoAxis * centerPadding, localToClipSpace);
                var axisEndInScreen = ObjectPosToScreenPos(gizmoAxis * length, localToClipSpace);


                var isHovering = false;
                if (CurrentDraggingMode == GizmoDraggingModes.None)
                {
                    isHovering = IsPointOnLine(mousePosInScreen, axisStartInScreen, axisEndInScreen);

                    if (isHovering && ImGui.IsMouseClicked(0))
                    {
                        CurrentDraggingMode = mode;
                        _offsetToOriginAtDragStart = mousePosInScreen - originInScreen;
                        _originAtDragStart = localToObject.TranslationVector;

                        var rayInObject = GetPickRayInObject(mousePosInScreen);
                        _plane = GetPlaneForDragMode(mode, rayInObject.Direction, _originAtDragStart);
                        _initialObjectToLocal = localToObject;
                        _initialObjectToLocal.Invert();
                        var rayInLocal = rayInObject;
                        rayInLocal.Direction = SharpDX.Vector3.TransformNormal(rayInObject.Direction, _initialObjectToLocal);
                        rayInLocal.Position = SharpDX.Vector3.TransformCoordinate(rayInObject.Position, _initialObjectToLocal);

                        if (!_plane.Intersects(ref rayInLocal, out _startIntersectionPoint))
                            Log.Debug($"Couldn't intersect pick ray with gizmo axis plane, something seems to be broken.");
                    }
                }
                else if (CurrentDraggingMode == mode)
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        CurrentDraggingMode = GizmoDraggingModes.None;
                    }
                    else
                    {
                        isHovering = true;

                        var rayInObject = GetPickRayInObject(mousePosInScreen);
                        var rayInLocal = rayInObject;
                        rayInLocal.Direction = SharpDX.Vector3.TransformNormal(rayInObject.Direction, _initialObjectToLocal);
                        rayInLocal.Position = SharpDX.Vector3.TransformCoordinate(rayInObject.Position, _initialObjectToLocal);

                        if (!_plane.Intersects(ref rayInLocal, out SharpDX.Vector3 intersectionPoint))
                            Log.Debug($"Couldn't intersect pick ray with gizmo axis plane, something seems to be broken.");

                        SharpDX.Vector3 offsetInLocal = (intersectionPoint - _startIntersectionPoint) * gizmoAxis;
                        var offsetInObject = SharpDX.Vector3.TransformNormal(offsetInLocal, localToObject);
                        SharpDX.Vector3 newOrigin = _originAtDragStart + offsetInObject;
                        transformable.Translation = new Vector3(newOrigin.X, newOrigin.Y, newOrigin.Z);
                    }
                }

                _drawList.AddLine(axisStartInScreen, axisEndInScreen, color, lineThickness * (isHovering ? 3 : 1));
                return isHovering;
            }

            // Returns true if hovered or active
            bool DoGizmoPlane(SharpDX.Vector3 gizmoAxis1, SharpDX.Vector3 gizmoAxis2, Color color, GizmoDraggingModes mode)
            {
                var origin = (gizmoAxis1 + gizmoAxis2) * centerPadding;
                Vector2[] pointsOnScreen =
                    {
                        ObjectPosToScreenPos(origin, localToClipSpace),
                        ObjectPosToScreenPos(origin + gizmoAxis1 * planeGizmoSize, localToClipSpace),
                        ObjectPosToScreenPos(origin + (gizmoAxis1 + gizmoAxis2) * planeGizmoSize, localToClipSpace),
                        ObjectPosToScreenPos(origin + gizmoAxis2 * planeGizmoSize, localToClipSpace),
                    };
                var isHovering = false;

                if (CurrentDraggingMode == GizmoDraggingModes.None)
                {
                    isHovering = IsPointInQuad(mousePosInScreen, pointsOnScreen);

                    if (isHovering && ImGui.IsMouseClicked(0))
                    {
                        CurrentDraggingMode = mode;
                        _offsetToOriginAtDragStart = mousePosInScreen - originInScreen;
                        _originAtDragStart = localToObject.TranslationVector;

                        var rayInObject = GetPickRayInObject(mousePosInScreen);
                        _plane = GetPlaneForDragMode(mode, rayInObject.Direction, _originAtDragStart);
                        _initialObjectToLocal = localToObject;
                        _initialObjectToLocal.Invert();
                        var rayInLocal = rayInObject;
                        rayInLocal.Direction = SharpDX.Vector3.TransformNormal(rayInObject.Direction, _initialObjectToLocal);
                        rayInLocal.Position = SharpDX.Vector3.TransformCoordinate(rayInObject.Position, _initialObjectToLocal);

                        if (!_plane.Intersects(ref rayInLocal, out _startIntersectionPoint))
                            Log.Debug($"Couldn't intersect pick ray with gizmo axis plane, something seems to be broken.");
                    }
                }
                else if (CurrentDraggingMode == mode)
                {
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        CurrentDraggingMode = GizmoDraggingModes.None;
                    }
                    else
                    {
                        isHovering = true;

                        var rayInObject = GetPickRayInObject(mousePosInScreen);
                        var rayInLocal = rayInObject;
                        rayInLocal.Direction = SharpDX.Vector3.TransformNormal(rayInObject.Direction, _initialObjectToLocal);
                        rayInLocal.Position = SharpDX.Vector3.TransformCoordinate(rayInObject.Position, _initialObjectToLocal);

                        if (!_plane.Intersects(ref rayInLocal, out SharpDX.Vector3 intersectionPoint))
                            Log.Debug($"Couldn't intersect pick ray with gizmo axis plane, something seems to be broken.");

                        SharpDX.Vector3 offsetInLocal = (intersectionPoint - _startIntersectionPoint);
                        var offsetInObject = SharpDX.Vector3.TransformNormal(offsetInLocal, localToObject);
                        SharpDX.Vector3 newOrigin = _originAtDragStart + offsetInObject;
                        transformable.Translation = new Vector3(newOrigin.X, newOrigin.Y, newOrigin.Z);
                    }
                }

                var color2 = color;
                color2.Rgba.W = isHovering ? 0.4f : 0.2f;
                _drawList.AddConvexPolyFilled(ref pointsOnScreen[0], 4, color2);
                return false;
            }

            void HandleDragInScreenSpace()
            {
                const float gizmoSize = 4;
                var screenSquaredMin = originInScreen - new Vector2(gizmoSize, gizmoSize);
                var screenSquaredMax = originInScreen + new Vector2(gizmoSize, gizmoSize);

                var isHovering = false;

                if (CurrentDraggingMode == GizmoDraggingModes.None)
                {
                    isHovering = (mousePosInScreen.X > screenSquaredMin.X && mousePosInScreen.X < screenSquaredMax.X &&
                                  mousePosInScreen.Y > screenSquaredMin.Y && mousePosInScreen.Y < screenSquaredMax.Y);
                    if (isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        CurrentDraggingMode = GizmoDraggingModes.PositionInScreenPlane;
                        _offsetToOriginAtDragStart = mousePosInScreen - originInScreen;
                    }
                }
                else if (CurrentDraggingMode == GizmoDraggingModes.PositionInScreenPlane)
                {
                    isHovering = true;
                    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                    {
                        CurrentDraggingMode = GizmoDraggingModes.None;
                    }
                    else
                    {
                        Vector2 newOriginInScreen = mousePosInScreen - _offsetToOriginAtDragStart;
                        // transform back to object space
                        var clipSpaceToObject = objectToClipSpace;
                        clipSpaceToObject.Invert();
                        var newOriginInCanvas = newOriginInScreen - topLeftOnScreen;
                        var newOriginInViewport = canvas.InverseTransformDirection(newOriginInCanvas);
                        var newOriginInClipSpace = new SharpDX.Vector4(2.0f * newOriginInViewport.X / viewport.Width - 1.0f,
                                                                       -(2.0f * newOriginInViewport.Y / viewport.Height - 1.0f),
                                                                       originInNdc.Z, 1);
                        var newOriginInObject = SharpDX.Vector4.Transform(newOriginInClipSpace, clipSpaceToObject);
                        Vector3 newTranslation = new Vector3(newOriginInObject.X, newOriginInObject.Y, newOriginInObject.Z) / newOriginInObject.W;
                        transformable.Translation = newTranslation;
                    }
                }
                var color2 = Color.Orange;
                color2.Rgba.W = isHovering ? 0.8f : 0.3f;
                _drawList.AddRectFilled(screenSquaredMin, screenSquaredMax, color2);
                //_drawList.AddConvexPolyFilled(ref pointsOnScreen[0], 4, color2);
            }

            Ray GetPickRayInObject(Vector2 posInScreen)
            {
                var clipSpaceToObject = objectToClipSpace;
                clipSpaceToObject.Invert();
                var newOriginInCanvas = posInScreen - topLeftOnScreen;
                var newOriginInViewport = canvas.InverseTransformDirection(newOriginInCanvas);

                float xInClipSpace = 2.0f * newOriginInViewport.X / viewport.Width - 1.0f;
                float yInClipSpace = -(2.0f * newOriginInViewport.Y / viewport.Height - 1.0f);

                var rayStartInClipSpace = new SharpDX.Vector3(xInClipSpace, yInClipSpace, 0);
                var rayStartInObject = SharpDX.Vector3.TransformCoordinate(rayStartInClipSpace, clipSpaceToObject);

                var rayEndInClipSpace = new SharpDX.Vector3(xInClipSpace, yInClipSpace, 1);
                var rayEndInObject = SharpDX.Vector3.TransformCoordinate(rayEndInClipSpace, clipSpaceToObject);

                var rayDir = (rayEndInObject - rayStartInObject);
                rayDir.Normalize();

                return new SharpDX.Ray(rayStartInObject, rayDir);
            }
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
        
        private static Vector2 ObjectPosToScreenPos(SharpDX.Vector3 posInObject, SharpDX.Matrix objectToClipSpace)
        {
            SharpDX.Vector3 originInClipSpace = SharpDX.Vector3.TransformCoordinate(posInObject, objectToClipSpace);
            Vector3 posInNdc = new Vector3(originInClipSpace.X, originInClipSpace.Y, originInClipSpace.Z);// / originInClipSpace.W;
            var viewports = ResourceManager.Instance().Device.ImmediateContext.Rasterizer.GetViewports<SharpDX.Mathematics.Interop.RawViewportF>();
            var viewport = viewports[0];
            var originInViewport = new Vector2(viewport.Width * (posInNdc.X * 0.5f + 0.5f),
                                               viewport.Height * (1.0f - (posInNdc.Y * 0.5f + 0.5f)));

            var canvas = ImageOutputCanvas.Current;
            var posInCanvas = canvas.TransformDirection(originInViewport);
            var topLeftOnScreen = ImageOutputCanvas.Current.TransformPosition(System.Numerics.Vector2.Zero);
            return topLeftOnScreen + posInCanvas;
        }

        private static SharpDX.Plane GetPlaneForDragMode(GizmoDraggingModes mode, SharpDX.Vector3 normDir, SharpDX.Vector3 origin)
        {
            switch (mode)
            {
                case GizmoDraggingModes.PositionXAxis:
                {
                    var secondAxis = Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitY)) < 0.5 
                                         ? SharpDX.Vector3.UnitY 
                                         : SharpDX.Vector3.UnitZ;
                    return new Plane(origin, origin + SharpDX.Vector3.UnitX, origin + secondAxis);
                }
                case GizmoDraggingModes.PositionYAxis:
                {
                    var secondAxis = Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitX)) < 0.5f 
                                         ? SharpDX.Vector3.UnitX 
                                         : SharpDX.Vector3.UnitZ;
                    return new Plane(origin, origin + SharpDX.Vector3.UnitY, origin + secondAxis);
                }
                case GizmoDraggingModes.PositionZAxis:
                {
                    var secondAxis = Math.Abs(SharpDX.Vector3.Dot(normDir, SharpDX.Vector3.UnitX)) < 0.5f 
                                         ? SharpDX.Vector3.UnitX 
                                         : SharpDX.Vector3.UnitY;
                    return new Plane(origin, origin + SharpDX.Vector3.UnitZ, origin + secondAxis);
                }
                case GizmoDraggingModes.PositionOnXyPlane:
                    return new Plane(origin, origin + SharpDX.Vector3.UnitX, origin + SharpDX.Vector3.UnitY);
                
                case GizmoDraggingModes.PositionOnXzPlane:
                    return new Plane(origin, origin + SharpDX.Vector3.UnitX, origin + SharpDX.Vector3.UnitZ);
                
                case GizmoDraggingModes.PositionOnYzPlane:
                    return new Plane(origin, origin + SharpDX.Vector3.UnitY, origin + SharpDX.Vector3.UnitZ);
            }

            Log.Error($"GetIntersectionPlane(...) called with wrong GizmoDraggingMode.");
            return new Plane(origin, SharpDX.Vector3.UnitX, SharpDX.Vector3.UnitY);
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
        
        private static bool IsPointInTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2) {
            var A = 0.5f * (-p1.Y * p2.X + p0.Y * (-p1.X + p2.X) + p0.X * (p1.Y - p2.Y) + p1.X * p2.Y);
            var sign = A < 0 ? -1 : 1;
            var s = (p0.Y * p2.X - p0.X * p2.Y + (p2.Y - p0.Y) * p.X + (p0.X - p2.X) * p.Y) * sign;
            var t = (p0.X * p1.Y - p0.Y * p1.X + (p0.Y - p1.Y) * p.X + (p1.X - p0.X) * p.Y) * sign;
    
            return s > 0 && t > 0 && (s + t) < 2 * A * sign;
        }
        
        private static bool IsPointInQuad(Vector2 p, Vector2[] corners)
        {
            return IsPointInTriangle(p, corners[0], corners[1], corners[2])
                   || IsPointInTriangle(p, corners[0], corners[2], corners[3]);
        }
        

        public enum GizmoDraggingModes
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

        private static readonly HashSet<ITransformable> SelectedTransformables = new HashSet<ITransformable>();
            
        private static Vector2 _offsetToOriginAtDragStart;
        private static SharpDX.Vector3 _originAtDragStart;
        private static Plane _plane;
        private static SharpDX.Vector3 _startIntersectionPoint;
        private static Matrix _initialObjectToLocal;


    }
}