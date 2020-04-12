using System;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Commands;
using T3.Gui.Interaction.Snapping;
using T3.Gui.UiHelpers;
using T3.Gui.Windows.TimeLine;
using UiHelpers;

namespace T3.Gui.InputUi
{
    public class CurveParameterCanvas : TimeCurveEditing, ICanvas, ITimeObjectManipulation
    {
        public CurveParameterCanvas()
        {
            _snapHandler.SnappedEvent += SnappedEventHandler;
            //_selectionFence = new TimeSelectionFence(this);
        }

        struct CanvasViewSettings
        {
            public Vector2 Scroll;
            public Vector2 Scale;
            public Vector2 ScrollTarget;
            public Vector2 ScaleTarget;
        }
        
        public void Draw(Curve curve)
        {
            _io = ImGui.GetIO();
            _mouse = ImGui.GetMousePos();
            _drawlist = ImGui.GetWindowDrawList();

            
            // Damp scaling
            const float dampSpeed = 30f;
            var damping = _io.DeltaTime * dampSpeed;
            if (!float.IsNaN(damping) && damping > 0.001f && damping <= 1.0f)
            {
                Scale = Im.Lerp(Scale, _scaleTarget, damping);
                Scroll = Im.Lerp(Scroll, _scrollTarget, damping);
            }
            
            ImGui.BeginChild("curve", new Vector2(0, 100), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                WindowPos = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos() + new Vector2(1, 1);
                WindowSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin() - new Vector2(2, 2);

                //ImGui.Text($"scroll: {this.Scroll}  scroll: {this.Scale}");
                ImGui.SetScrollY(0);
                THelpers.DebugContentRect();

                HandleInteraction();
                _standardRaster.Draw(this);
                _horizontalRaster.Draw(this);
                TimelineCurveEditArea.DrawCurveLine(curve, this);
                
                foreach (var keyframe in curve.GetVDefinitions())
                {
                    //CurvePoint.Draw(keyframe, this,  SelectedKeyframes.Contains(keyframe), this);
                    CurvePoint.Draw(keyframe, this,  false, null);
                }
                DrawSnapIndicator();
            }
            ImGui.EndChild();
        }

        private void HandleInteraction()
        {
            if (!ImGui.IsWindowHovered())
                return;

            if (ImGui.IsMouseDragging(1))
            {
                _scrollTarget -= InverseTransformDirection(_io.MouseDelta);
            }

            if (Math.Abs(_io.MouseWheel) > 0.01f)
                HandleZoomViewWithMouseWheel();

            // if (KeyboardBinding.Triggered(UserActions.DeleteSelection))
            //     DeleteSelectedElements();
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

        public void SetVisibleValueRange(float valueScale, float valueScroll)
        {
            _scaleTarget = new Vector2(_scaleTarget.X, valueScale);
            _scrollTarget = new Vector2(_scrollTarget.X, valueScroll);
        }

        public void SetVisibleTimeRange(float timeScale, float timeScroll)
        {
            _scaleTarget = new Vector2(timeScale, _scaleTarget.Y);
            _scrollTarget = new Vector2(timeScroll, _scrollTarget.Y);
        }

        private void SnappedEventHandler(double snapPosition)
        {
            _lastSnapTime = ImGui.GetTime();
            _lastSnapU = (float)snapPosition;
        }

        private void DrawSnapIndicator()
        {
            var opacity = 1 - ((float)(ImGui.GetTime() - _lastSnapTime) / SnapIndicatorDuration).Clamp(0, 1);
            var color = Color.Orange;
            color.Rgba.W = opacity;
            var p = new Vector2(TransformPositionX(_lastSnapU), 0);
            _drawlist.AddRectFilled(p, p + new Vector2(1, 2000), color);
        }

        private double _lastSnapTime;
        private const float SnapIndicatorDuration = 1;
        private float _lastSnapU;
        

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
        public float TransformPositionX(float xOnCanvas)
        {
            var scale = Scale.X ;
            var offset = Scroll.X ;
            return (int)((xOnCanvas - offset) * scale + WindowPos.X);
        }

        public float TransformGlobalTime(float time)
        {
            var localTime = (time) ;
            return TransformPositionX(localTime);
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
        public float InverseTransformPositionX(float xOnScreen)
        {
            var scale = Scale.X ;
            var offset = Scroll.X;
            return (xOnScreen - WindowPos.X) / scale + offset;
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
        
        public Vector2 WindowPos { get; private set; }
        public Vector2 WindowSize { get; private set; }
        #endregion
        
        private readonly StandardTimeRaster _standardRaster = new StandardTimeRaster();
        private readonly HorizontalRaster _horizontalRaster = new HorizontalRaster();
        private readonly ValueSnapHandler _snapHandler = new ValueSnapHandler();
        
        private ImGuiIOPtr _io;
        private Vector2 _mouse;
        public Vector2 Scroll { get; private set; } = new Vector2(-1, 2.5f);
        private Vector2 _scrollTarget = new Vector2(-1, 2.5f);
        
        public Vector2 Scale { get; private set; } = new Vector2(40, -80);
        private Vector2 _scaleTarget = new Vector2(40, -80);
        private ImDrawListPtr _drawlist;

        #region implement selection holder
        public void ClearSelection()
        {
            throw new NotImplementedException();
        }

        public void UpdateSelectionForArea(ImRect area, SelectMode selectMode)
        {
            throw new NotImplementedException();
        }

        public void DeleteSelectedElements()
        {
            throw new NotImplementedException();
        }

        public ICommand StartDragCommand()
        {
            throw new NotImplementedException();
        }

        public void UpdateDragCommand(double dt, double dv)
        {
            throw new NotImplementedException();
        }

        public void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            throw new NotImplementedException();
        }

        public void CompleteDragCommand()
        {
            throw new NotImplementedException();
        }

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
            throw new NotImplementedException();
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
            throw new NotImplementedException();
        }

        public TimeRange GetSelectionTimeRange()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}