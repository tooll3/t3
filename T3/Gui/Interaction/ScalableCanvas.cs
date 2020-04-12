using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Selection;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Interaction
{
    /// <summary>
    /// Implements transformations and interactions for a canvas that can
    /// be zoomed and panned.
    /// </summary>
    public abstract class ScalableCanvas : ICanvas
    {
        protected float ZoomSpeed = 12;
        
        /// <summary>
        /// This needs to be called by the inherited class before drawing its interface. 
        /// </summary>
        protected void UpdateCanvas()
        {
            _io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();

            InitWindowSize();

            // Damp scaling
            var p1 = Scroll;
            var p2 = Scroll + WindowSize * Scale;
            var p1Target = _scrollTarget;
            var p2Target = _scrollTarget + WindowSize * _scaleTarget;
            //var speed = 12;
            var f = Math.Min(_io.DeltaTime * ZoomSpeed, 1);
            var pp1 = Im.Lerp(p1, p1Target, f);
            var pp2 = Im.Lerp(p2, p2Target, f);
            var scaleT =   (pp2 - pp1) / WindowSize;
            
            Scale = scaleT;
            Scroll = pp1;

            if (!ImGui.IsWindowHovered())
                return;

            if (!NoMouseInteraction 
                && (ImGui.IsMouseDragging(1) 
                || (ImGui.IsMouseDragging(0) && ImGui.GetIO().KeyAlt)))
            {
                _scrollTarget += _io.MouseDelta;
                UserScrolledCanvas = true;
            }
            else
            {
                UserScrolledCanvas = false;
            }

            HandleZoomInteraction();

            //ImGui.SetScrollY(0);    // HACK: prevent jump of scroll position by accidental scrolling
        }

        protected void InitWindowSize()
        {
            WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
            WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);
        }

        public CanvasProperties GetTargetProperties()
        {
            return new CanvasProperties()
                   {
                       Scale = _scaleTarget,
                       Scroll = _scrollTarget
                   };
        }


        #region implement ICanvas =================================================================

        public abstract IEnumerable<ISelectableNode> SelectableChildren { get; }

        /// <summary>
        /// Get screen position applying canvas zoom and scrolling to graph position (e.g. of an Operator) 
        /// </summary>
        public Vector2 TransformPosition(Vector2 posOnCanvas)
        {
            return posOnCanvas * Scale + Scroll + WindowPos;
        }
        
        public Vector2 TransformPositionFloored(Vector2 posOnCanvas)
        {
            return Im.Floor((posOnCanvas - Scroll) * Scale + WindowPos);
        }


        /// <summary>
        /// Convert at screen space position (e.g. from mouse) to canvas coordinates applying canvas zoom and scrolling 
        /// </summary>
        public Vector2 InverseTransformPosition(Vector2 screenPos)
        {
            return (screenPos - Scroll - WindowPos) / Scale;
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
        private Vector2 _scaleTarget = Vector2.One;

        public Vector2 Scroll { get; private set; } = new Vector2(0.0f, 0.0f);
        private Vector2 _scrollTarget = new Vector2(0.0f, 0.0f);
        #endregion

        public void SetScaleToMatchPixels()
        {
            _scaleTarget = Vector2.One;
        }

        public void FitAreaOnCanvas(ImRect area)
        {
            var height = area.GetHeight();
            var width = area.GetWidth();
            var targetAspect = width / height;

            // Use a fallback resolution to fix initial call from constructor
            // where img has not been initialized yet.
            if (WindowSize == Vector2.Zero)
            {
                WindowSize = new Vector2(800,500);
            }

            float scale;
            if (targetAspect > WindowSize.X / WindowSize.Y)
            {
                scale = WindowSize.X / width;
                _scrollTarget = new Vector2(
                                            -area.Min.X * scale, 
                                            -area.Min.Y* scale+ (WindowSize.Y - height * scale) / 2);
            }
            else
            {
                scale = WindowSize.Y / height;
                _scrollTarget = new Vector2(
                                            -area.Min.X* scale + (WindowSize.X - width * scale) / 2, 
                                            -area.Min.Y * scale);
            }
            _scaleTarget = new Vector2(scale, scale);
        }

        public enum Transition
        {
            JumpIn,
            JumpOut,
            Undefined,
        }

        protected void SetAreaWithTransition(Vector2 scale, Vector2 scroll, Vector2 previousFocusOnScreen, Transition transition)
        {
            _scaleTarget = scale;
            Scale = scale * (transition == Transition.JumpIn ? 0.3f : 1.5f);
            
            _scrollTarget = scroll;
            if(transition == Transition.JumpIn)
                Scroll = _scrollTarget+ WindowSize * 0.5f;
            else
            {
                var delta = WindowSize / 2;
                var scrollIs = delta * Scale;
                Scroll = _scrollTarget -scrollIs;
            }
        }

        private void HandleZoomInteraction()
        {
            if (NoMouseInteraction)
                return;
            
            UserZoomedCanvas = false;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_io.MouseWheel == 0)
                return;

            const float zoomSpeed = 1.2f;
            var focusCenter = (_mouse - Scroll - WindowPos) / Scale;
            
            var zoomDelta = 1f;

            if (_io.MouseWheel < 0.0f)
            {
                for (float zoom = _io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                {
                    zoomDelta /= zoomSpeed;
                }
                UserZoomedCanvas = true;
            }

            if (_io.MouseWheel > 0.0f)
            {
                for (float zoom = _io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                {
                    zoomDelta *= zoomSpeed;
                }
                UserZoomedCanvas = true;
            }
            _scaleTarget *= zoomDelta;

            Vector2 shift = _scrollTarget + (focusCenter * _scaleTarget);
            _scrollTarget += _mouse - shift - WindowPos;
        }

        
        public struct CanvasProperties
        {
            public Vector2 Scale;
            public Vector2 Scroll;
        }
        
        protected bool UserZoomedCanvas;
        protected bool UserScrolledCanvas;
        public bool NoMouseInteraction;
        private Vector2 _mouse;
        private ImGuiIOPtr _io;
    }
}
