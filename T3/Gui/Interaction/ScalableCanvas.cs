using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Logging;
using T3.Gui.UiHelpers;
using UiHelpers;

namespace T3.Gui.Interaction
{
    /// <summary>
    /// Implements transformations and interactions for a canvas that can
    /// be zoomed and panned.
    /// </summary>
    public class ScalableCanvas : ICanvas
    {
        protected float ZoomSpeed = 12;

        /// <summary>
        /// This needs to be called by the inherited class before drawing its interface. 
        /// </summary>
        public void UpdateCanvas()
        {
            Io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();

            WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
            WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);
            DampScaling();
            HandleInteraction();
        }



        protected void DampScaling()
        {
            // Damp scaling
            var p1 = Scroll;
            var p2 = Scroll + WindowSize * Scale;
            var p1Target = ScrollTarget;
            var p2Target = ScrollTarget + WindowSize * ScaleTarget;
            var f = Math.Min(Io.DeltaTime * ZoomSpeed, 1);
            var pp1 = Im.Lerp(p1, p1Target, f);
            var pp2 = Im.Lerp(p2, p2Target, f);
            var scaleT = (pp2 - pp1) / WindowSize;

            Scale = scaleT;
            Scroll = pp1;
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
            return posOnCanvas * Scale + Scroll + WindowPos;
        }

        public Vector2 TransformPositionFloored(Vector2 posOnCanvas)
        {
            return Im.Floor(posOnCanvas * Scale + Scroll + WindowPos);
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
            return TransformPosition(new Vector2(0,yOnCanvas)).Y;
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

        public void SetScopeToCanvasArea(ImRect area, bool flipY = false)
        {
            WindowSize = ImGui.GetContentRegionMax()- ImGui.GetWindowContentRegionMin();
            ScaleTarget = WindowSize / area.GetSize();
            if (flipY)
            {
                ScaleTarget.Y *= -1;
            }
            
            ScrollTarget = new Vector2(-area.Min.X * ScaleTarget.X,
                                       -area.Max.Y * ScaleTarget.Y);
        }
        
        public void FitAreaOnCanvas(ImRect area, bool flipY=false)
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

        protected virtual void HandleInteraction()
        {
            if (!ImGui.IsWindowHovered())
                return;

            if (!NoMouseInteraction
                && (ImGui.IsMouseDragging(ImGuiMouseButton.Right)
                    || (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && ImGui.GetIO().KeyAlt)))
            {
                ScrollTarget += Io.MouseDelta;
                UserScrolledCanvas = true;
            }
            else
            {
                UserScrolledCanvas = false;
            }

            HandleZoomWithMouseWheel();
        }

        private void HandleZoomWithMouseWheel()
        {
            UserZoomedCanvas = false;

            if (Math.Abs(Io.MouseWheel) < 0.01f)
                return;
            
            var focusCenter = (_mouse - Scroll - WindowPos) / Scale;
            var zoomDelta = ComputeZoomDeltaFromMouseWheel();
            

            if (IsCurveCanvas)
            {
                if (ImGui.GetIO().KeyCtrl)
                {
                    ScaleTarget.X *= zoomDelta;
                }
                else if(ImGui.GetIO().KeyShift)
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
            if (Io.MouseWheel < 0.0f)
            {
                for (var zoom = Io.MouseWheel; zoom < 0.0f; zoom += 1.0f)
                {
                    zoomSum /= zoomSpeed;
                }
            }

            if (Io.MouseWheel > 0.0f)
            {
                for (var zoom = Io.MouseWheel; zoom > 0.0f; zoom -= 1.0f)
                {
                    zoomSum *= zoomSpeed;
                }
            }

            zoomSum = zoomSum.Clamp(0.01f, 100f);
            return zoomSum;
        }

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
        public bool NoMouseInteraction;
        private Vector2 _mouse;
        protected ImGuiIOPtr Io;
    }
}