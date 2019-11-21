using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.Selection;
using UiHelpers;

namespace T3.Gui.Graph
{
    /// <summary>
    /// Implements transformations and interactions for a canvas that can
    /// be zoomed and panned.
    /// </summary>
    public abstract class ScalableCanvas : ICanvas
    {
        /// <summary>
        /// This needs to be called by the inherited class before drawing its interface. 
        /// </summary>
        protected void UpdateCanvas()
        {
            _io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();

            WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
            WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);

            // Damp scaling
            Scale = Im.Lerp(Scale, _scaleTarget, _io.DeltaTime * 10);
            Scroll = Im.Lerp(Scroll, _scrollTarget, _io.DeltaTime * 10);

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

            ImGui.SetScrollY(0);    // HACK: prevent jump of scroll position by accidental scrolling
        }


        #region implement ICanvas =================================================================

        public abstract IEnumerable<ISelectable> SelectableChildren { get; }
        public abstract SelectionHandler SelectionHandler { get; }

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

        protected void FitArea(ImRect area)
        {
            var height = area.GetHeight();
            var width = area.GetWidth();
            var targetAspect = width / height;

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

        protected void SetAreaWithTransition(Vector2 scale, Vector2 scroll, bool zoomIn = true)
        {
            _scaleTarget = scale;
            Scale = scale * (zoomIn ? 0.3f : 3);
            
            _scrollTarget = scroll;
            if(zoomIn)
                Scroll = _scrollTarget+ WindowSize * 0.5f;
            else
            {
                Scroll = _scrollTarget - WindowSize * 0.5f;
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

        protected bool UserZoomedCanvas;
        protected bool UserScrolledCanvas;
        public bool NoMouseInteraction;
        private Vector2 _mouse;
        private ImGuiIOPtr _io;
    }
}
