using System;
using System.Numerics;
using ImGuiNET;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Windows.TimeLine
{
    public abstract class CurveCanvas : ICanvas
    {
        #region implement ICanvas =================================================================
        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            return (posOnCanvas - Scroll) * Scale + WindowPos;
        }

        public Vector2 TransformPositionFloored(Vector2 posOnCanvas)
        {
            return Im.Floor((posOnCanvas - Scroll) * Scale + WindowPos);
        }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public virtual float TransformPositionX(float xOnCanvas)
        {
            return (int)((xOnCanvas - Scroll.X) * Scale.X + WindowPos.X);
        }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public float TransformPositionY(float yOnCanvas)
        {
            return (yOnCanvas - Scroll.Y) * Scale.Y + WindowPos.Y;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public Vector2 InverseTransformPosition(Vector2 posOnScreen)
        {
            return (posOnScreen - WindowPos) / Scale + Scroll;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public virtual float InverseTransformPositionX(float xOnScreen)
        {
            return (xOnScreen - WindowPos.X) / Scale.X + Scroll.X;
        }

        /// <summary>
        /// Convert screen position to canvas position
        /// </summary>
        public float InverseTransformPositionY(float yOnScreen)
        {
            return (yOnScreen - WindowPos.Y) / Scale.Y + Scroll.Y;
        }

        /// <summary>
        /// Convert direction on canvas to delta in screen space
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

        /// <summary>
        /// Convert rectangle on canvas to screen space
        /// </summary>
        public ImRect TransformRect(ImRect canvasRect)
        {
            var r = new ImRect(TransformPositionFloored(canvasRect.Min), TransformPositionFloored(canvasRect.Max));
            if (r.Min.Y > r.Max.Y)
            {
                var t = r.Min.Y;
                r.Min.Y = r.Max.Y;
                r.Max.Y = t;
            }

            return r;
        }

        public ImRect InverseTransformRect(ImRect screenRect)
        {
            var r = new ImRect(InverseTransformPosition(screenRect.Min), InverseTransformPosition(screenRect.Max));
            if (!(r.Min.Y > r.Max.Y))
                return r;

            var t = r.Min.Y;
            r.Min.Y = r.Max.Y;
            r.Max.Y = t;
            return r;
        }

        /// <summary>
        /// Damped scale factors for u and v
        /// </summary>
        public Vector2 Scale { get; private set; } = new Vector2(1, -1);

        public Vector2 WindowPos { get; private set; }
        public Vector2 WindowSize { get; private set; }
        public Vector2 Scroll { get; private set; } = new Vector2(0, 0.0f);
        #endregion

        protected void Update()
        {
            _io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();

            WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
            WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);

            // Damp scaling
            const float dampSpeed = 30f;
            var damping = ImGui.GetIO().DeltaTime * dampSpeed;
            if (!float.IsNaN(damping) && damping > 0.001f && damping <= 1.0f)
            {
                Scale = Im.Lerp(Scale, _scaleTarget, damping);
                Scroll = Im.Lerp(Scroll, _scrollTarget, damping);
            }
        }

        protected void HandleInteraction()
        {
            if (!ImGui.IsWindowHovered())
                return;

            if (ImGui.IsMouseDragging(1))
            {
                _scrollTarget -= InverseTransformDirection(_io.MouseDelta);
            }

            if (Math.Abs(_io.MouseWheel) > 0.01f)
                HandleZoomViewWithMouseWheel();
        }

        private void HandleZoomViewWithMouseWheel()
        {
            var zoomDelta = ComputeZoomDeltaFromMouseWheel();
            var uAtMouse = InverseTransformDirection(_mouse - WindowPos);
            var uScaled = uAtMouse / zoomDelta;
            var deltaU = uScaled - uAtMouse;

            if (_io.KeyShift)
            {
                _scrollTarget.Y -= deltaU.Y;
                _scaleTarget.Y *= zoomDelta;
            }
            else
            {
                _scrollTarget.X -= deltaU.X;
                _scaleTarget.X *= zoomDelta;
            }
        }

        private float ComputeZoomDeltaFromMouseWheel()
        {
            const float zoomSpeed = 1.2f;
            var zoomSum = 1f;
            if (_io.MouseWheel < 0.0f)
            {
                for (var zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                {
                    zoomSum /= zoomSpeed;
                }
            }

            if (_io.MouseWheel > 0.0f)
            {
                for (var zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                {
                    zoomSum *= zoomSpeed;
                }
            }

            zoomSum = zoomSum.Clamp(0.01f, 100f);
            return zoomSum;
        }

        public void SetVisibleRange(Vector2 scale, Vector2 scroll)
        {
            _scaleTarget = scale;
            _scrollTarget = scroll;
        }

        public void SetVisibleVRange(float valueScale, float valueScroll)
        {
            _scaleTarget = new Vector2(_scaleTarget.X, valueScale);
            _scrollTarget = new Vector2(_scrollTarget.X, valueScroll);
        }

        public void SetVisibleTimeRange(float timeScale, float timeScroll)
        {
            _scaleTarget = new Vector2(timeScale, _scaleTarget.Y);
            _scrollTarget = new Vector2(timeScroll, _scrollTarget.Y);
        }

        private Vector2 _scrollTarget = new Vector2(-25f, 0.0f);
        private Vector2 _scaleTarget = new Vector2(10, -1);

        protected ImGuiIOPtr _io;
        private Vector2 _mouse;
    }
}