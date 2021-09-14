using System;
using System.Numerics;
using ImGuiNET;
using T3.Core;
using T3.Core.Logging;
using T3.Gui.Graph;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.Interaction
{
    /// <summary>
    /// Implements transformations and interactions for a canvas that can
    /// be zoomed and panned.
    /// </summary>
    public class ScalableCanvas : ICanvas
    {
        //protected float UserSetting. = 12;

        /// <summary>
        /// This needs to be called by the inherited class before drawing its interface. 
        /// </summary>
        public void UpdateCanvas(T3Ui.EditingFlags flags = T3Ui.EditingFlags.None)
        {
            Io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();

            WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
            WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);
            DampScaling();
            HandleInteraction(flags);
        }

        protected void DampScaling()
        {
            // Damp scaling
            var p1 = Scroll;
            var p2 = Scroll + WindowSize * Scale;
            var p1Target = ScrollTarget;
            var p2Target = ScrollTarget + WindowSize * ScaleTarget;
            var f = Math.Min(Io.DeltaTime / UserSettings.Config.ScrollSmoothing.Clamp(0.01f, 0.99f), 1);
            var pp1 = Vector2.Lerp(p1, p1Target, f);
            var pp2 = Vector2.Lerp(p2, p2Target, f);
            var scaleT = (pp2 - pp1) / WindowSize;

            Scale = scaleT;
            Scroll = pp1;

            var completed = Math.Abs(Scroll.X - ScrollTarget.X) < 1f
                            && Math.Abs(Scroll.Y - ScrollTarget.Y) < 1f
                            && Math.Abs(Scale.X - ScaleTarget.X) < 0.05f
                            && Math.Abs(Scale.Y - ScaleTarget.Y) < 0.05f;

            if (completed)
            {
                Scroll = ScrollTarget;
                Scale = ScaleTarget;
            }
        }

        public Scope GetTargetScope()
        {
            return new Scope()
                       {
                           Scale = ScaleTarget,
                           Scroll = ScrollTarget
                       };
        }

        public void SetVisibleRange(Vector2 scale, Vector2 scroll)
        {
            ScaleTarget = scale;
            ScrollTarget = scroll;
        }

        public void SetVisibleVRange(float valueScale, float valueScroll)
        {
            ScaleTarget = new Vector2(ScaleTarget.X, valueScale);
            ScrollTarget = new Vector2(ScrollTarget.X, valueScroll);
        }

        #region implement ICanvas =================================================================
        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public virtual Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            var v = posOnCanvas * Scale + Scroll + WindowPos;
            return new Vector2((int)v.X, (int)v.Y);
        }

        public Vector2 TransformPositionFloored(Vector2 posOnCanvas)
        {
            return MathUtils.Floor(posOnCanvas * Scale + Scroll + WindowPos);
        }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public float TransformX(float xOnCanvas)
        {
            return TransformPosition(new Vector2(xOnCanvas, 0)).X;
        }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public float TransformY(float yOnCanvas)
        {
            return TransformPosition(new Vector2(0, yOnCanvas)).Y;
        }

        /// <summary>
        /// Convert at screen space position (e.g. from mouse) to canvas coordinates applying canvas zoom and scrolling 
        /// </summary>
        public virtual Vector2 InverseTransformPosition(Vector2 screenPos)
        {
            return (screenPos - Scroll - WindowPos) / Scale;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public virtual float InverseTransformX(float xOnScreen)
        {
            return InverseTransformPosition(new Vector2(xOnScreen, 0)).X;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public float InverseTransformY(float yOnScreen)
        {
            //return (yOnScreen - WindowPos.Y) / Scale.Y + Scroll.Y;
            //return (yOnScreen - WindowPos.Y - WindowPos.Y) / Scale.Y;
            return InverseTransformPosition(new Vector2(yOnScreen, 0)).Y;
        }

        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 TransformDirection(Vector2 vectorInCanvas)
        {
            return vectorInCanvas * Scale;
        }

        /// <summary>
        /// Convert a direction (e.g. MouseDelta) from ScreenSpace to Canvas
        /// </summary>
        public Vector2 InverseTransformDirection(Vector2 vectorInScreen)
        {
            return vectorInScreen / Scale;
        }

        public ImRect TransformRect(ImRect canvasRect)
        {
            return new ImRect(TransformPosition(canvasRect.Min), TransformPosition(canvasRect.Max));
        }

        public ImRect InverseTransformRect(ImRect screenRect)
        {
            return new ImRect(InverseTransformPosition(screenRect.Min), InverseTransformPosition(screenRect.Max));
        }

        /// <summary>
        /// Transform a canvas position to relative position within ImGui-window (e.g. to set ImGui context) 
        /// </summary>
        public Vector2 ChildPosFromCanvas(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale + Scroll;
        }

        public Vector2 WindowPos { get; private set; }
        public Vector2 WindowSize { get; private set; }

        public Vector2 Scale { get; private set; } = Vector2.One;
        protected Vector2 ScaleTarget = Vector2.One;

        public Vector2 Scroll { get; private set; } = new Vector2(0.0f, 0.0f);
        protected Vector2 ScrollTarget = new Vector2(0.0f, 0.0f);
        #endregion

        public void SetScaleToMatchPixels()
        {
            ScaleTarget = Vector2.One;
        }

        public void SetScaleToParentCanvas(ScalableCanvas parent)
        {
            if (parent == null)
                return;

            Scale = ScaleTarget * parent.Scale;
            Scroll = ScrollTarget * parent.Scale;
        }

        public void SetScopeToCanvasArea(ImRect area, bool flipY = false, ScalableCanvas parent = null)
        {
            WindowSize = ImGui.GetContentRegionMax() - ImGui.GetWindowContentRegionMin();
            ScaleTarget = WindowSize / area.GetSize();

            if (flipY)
            {
                ScaleTarget.Y *= -1;
            }

            if (parent != null)
            {
                ScaleTarget /= parent.Scale;
            }
            
            ScrollTarget = new Vector2(-area.Min.X * ScaleTarget.X,
                                       -area.Max.Y * ScaleTarget.Y);
        }
        
        public void SetVerticalScopeToCanvasArea(ImRect area, bool flipY = false, ScalableCanvas parent = null)
        {
            WindowSize = ImGui.GetContentRegionMax() - ImGui.GetWindowContentRegionMin();
            ScaleTarget.Y = WindowSize.Y / area.GetSize().Y;

            if (flipY)
            {
                ScaleTarget.Y *= -1;
            }

            if (parent != null)
            {
                ScaleTarget.Y /= parent.Scale.Y;
            }
            
            ScrollTarget.Y = -area.Max.Y * ScaleTarget.Y;
        }

        public void FitAreaOnCanvas(ImRect area, bool flipY = false)
        {
            var height = area.GetHeight();
            var width = area.GetWidth();
            var targetAspect = width / height;

            // Use a fallback resolution to fix initial call from constructor
            // where img has not been initialized yet.
            if (WindowSize == Vector2.Zero)
            {
                WindowSize = new Vector2(800, 500);
            }

            float scale;
            if (targetAspect > WindowSize.X / WindowSize.Y)
            {
                scale = WindowSize.X / width;
                ScrollTarget = new Vector2(
                                           -area.Min.X * scale,
                                           -area.Min.Y * scale + (WindowSize.Y - height * scale) / 2);
            }
            else
            {
                scale = WindowSize.Y / height;
                ScrollTarget = new Vector2(
                                           -area.Min.X * scale + (WindowSize.X - width * scale) / 2,
                                           -area.Min.Y * scale);
            }

            ScaleTarget = new Vector2(scale, scale);
            if (flipY)
            {
                ScaleTarget.Y *= -1;
            }
        }

        public enum Transition
        {
            JumpIn,
            JumpOut,
            Undefined,
        }

        protected void SetScopeWithTransition(Vector2 scale, Vector2 scroll, Vector2 previousFocusOnScreen, Transition transition)
        {
            if (float.IsInfinity(scale.X) || float.IsNaN(scale.X)
                                          || float.IsInfinity(scale.Y) || float.IsNaN(scale.Y)
                                          || float.IsInfinity(scroll.X) || float.IsNaN(scroll.X)
                                          || float.IsInfinity(scroll.Y) || float.IsNaN(scroll.Y)
                )
            {
                scale = Vector2.One;
                scroll = Vector2.Zero;
            }

            ScaleTarget = scale;
            Scale = scale * (transition == Transition.JumpIn ? 0.3f : 1.5f);

            ScrollTarget = scroll;
            if (transition == Transition.JumpIn)
                Scroll = ScrollTarget + WindowSize * 0.5f;
            else
            {
                var delta = WindowSize / 2;
                var scrollIs = delta * Scale;
                Scroll = ScrollTarget - scrollIs;
            }
        }

        protected virtual void HandleInteraction(T3Ui.EditingFlags flags)
        {
            var isDraggingConnection = (ConnectionMaker.TempConnections.Count > 0) && ImGui.IsWindowFocused();

            if (!ImGui.IsWindowHovered() && !isDraggingConnection)
                return;

            if (PreventMouseInteraction)
                return;

            if ((flags & T3Ui.EditingFlags.PreventPanningWithMouse) == 0
                && (
                       ImGui.IsMouseDragging(ImGuiMouseButton.Right)
                       || ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.GetIO().KeyAlt)
                )
            {
                ScrollTarget += Io.MouseDelta;
                UserScrolledCanvas = true;
            }
            else
            {
                UserScrolledCanvas = false;
            }

            if ((flags & T3Ui.EditingFlags.PreventZoomWithMouseWheel) == 0)
            {
                ZoomWithMouseWheel();
                ZoomWithMiddleMouseDrag();

                if (!IsCurveCanvas)
                {
                    if (this is TimeLineCanvas)
                    {
                        ScaleTarget.X = ScaleTarget.X.Clamp(0.01f, 5000);
                        ScaleTarget.Y = ScaleTarget.Y.Clamp(0.01f, 5000);
                    }
                    else
                    {
                        ScaleTarget.X = ScaleTarget.X.Clamp(0.1f, 30);
                        ScaleTarget.Y = ScaleTarget.Y.Clamp(0.1f, 30);
                    }
                }
            }
        }

        private void ZoomWithMouseWheel()
        {
            UserZoomedCanvas = false;

            var focusCenter = (_mouse - ScrollTarget - WindowPos) / ScaleTarget;
            var zoomDelta = ComputeZoomDeltaFromMouseWheel();

            if (Math.Abs(zoomDelta - 1) < 0.001f)
                return;

            if (IsCurveCanvas)
            {
                if (ImGui.GetIO().KeyAlt)
                {
                    ScaleTarget.X *= zoomDelta;
                }
                else if (ImGui.GetIO().KeyShift)
                {
                    ScaleTarget.Y *= zoomDelta;
                }
                else
                {
                    ScaleTarget *= zoomDelta;
                }
            }
            else
            {
                ScaleTarget *= zoomDelta;
            }

            if (Math.Abs(zoomDelta) > 0.1f)
                UserZoomedCanvas = true;

            var shift = ScrollTarget + (focusCenter * ScaleTarget);
            ScrollTarget += _mouse - shift - WindowPos;
        }

        private bool IsCurveCanvas => Scale.Y < 0;

        private float ComputeZoomDeltaFromMouseWheel()
        {
            const float zoomSpeed = 1.2f;
            var zoomSum = 1f;
            var ioMouseWheel = Io.MouseWheel;

            if (ioMouseWheel < 0.0f)
            {
                for (var zoom = ioMouseWheel; zoom < 0.0f; zoom += 1.0f)
                {
                    zoomSum /= zoomSpeed;
                }
            }

            if (ioMouseWheel > 0.0f)
            {
                for (var zoom = ioMouseWheel; zoom > 0.0f; zoom -= 1.0f)
                {
                    zoomSum *= zoomSpeed;
                }
            }

            zoomSum = zoomSum.Clamp(0.02f, 100f);
            return zoomSum;
        }

        private void ZoomWithMiddleMouseDrag()
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
            {
                _mousePosWhenMiddlePressed = ImGui.GetMousePos();
                _scaleWhenMiddlePressed = ScaleTarget;
            }

            if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle, 0))
            {
                var delta = ImGui.GetMousePos() - _mousePosWhenMiddlePressed;
                var deltaMax = Math.Abs(delta.X) > Math.Abs(delta.Y)
                                   ? -delta.X
                                   : delta.Y;
                if (IsCurveCanvas)
                {
                }
                else
                {
                    var f = (float)Math.Pow(1.1f, -deltaMax / 40f);
                    ScaleTarget = _scaleWhenMiddlePressed * f;
                }

                var focusCenter = (_mousePosWhenMiddlePressed - Scroll - WindowPos) / Scale;
                var shift = ScrollTarget + (focusCenter * ScaleTarget);
                ScrollTarget += _mousePosWhenMiddlePressed - shift - WindowPos;
            }
        }

        private Vector2 _mousePosWhenMiddlePressed;
        private Vector2 _scaleWhenMiddlePressed;

        public struct Scope
        {
            public Scope(Vector2 scale, Vector2 scroll)
            {
                Scale = scale;
                Scroll = scroll;
            }

            public Vector2 Scale;
            public Vector2 Scroll;
        }

        protected bool UserZoomedCanvas;
        protected bool UserScrolledCanvas;
        public bool PreventMouseInteraction;
        private Vector2 _mouse;
        protected ImGuiIOPtr Io;
    }
}