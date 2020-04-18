using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Windows.TimeLine;
using UiHelpers;
using System;
using T3.Gui.Commands;

namespace T3.Gui.Interaction
{
    public abstract class CurveEditCanvas: ScalableCanvas, ITimeObjectManipulation
    {
        public CurveEditCanvas()
        {
            ScrollTarget = new Vector2(500f, 0.0f);
            ScaleTarget = new Vector2(80, -1);

            _snapHandler.SnappedEvent += SnappedEventHandler;
        }
        

        public void DrawCurveCanvas(Action drawAdditionalCanvasContent)
        {
            _drawlist = ImGui.GetWindowDrawList();
            
            ImGui.BeginChild("timeline", new Vector2(0, 0), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                UpdateCanvas();
                _drawlist = ImGui.GetWindowDrawList();

                drawAdditionalCanvasContent();
                DrawSnapIndicator();
            }
            ImGui.EndChild();
        }

        
        private void SnappedEventHandler(double snapPosition)
        {
            _lastSnapTime = ImGui.GetTime();
            _lastSnapU = (float)snapPosition;
        }

        private void DrawSnapIndicator()
        {
            var opacity = 1 - ((float)(ImGui.GetTime() - _lastSnapTime) / _snapIndicatorDuration).Clamp(0, 1);
            var color = Color.Orange;
            color.Rgba.W = opacity;
            var p = new Vector2(TransformX(_lastSnapU), 0);
            _drawlist.AddRectFilled(p, p + new Vector2(1, 2000), color);
        }
        
        
        #region implement ITimeObjectManipulation to forward interaction to children
        public void ClearSelection()
        {
            foreach (var sh in _selectionHolders)
            {
                sh.ClearSelection();
            }
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectMode selectMode)
        {
            foreach (var sh in _selectionHolders)
            {
                sh.UpdateSelectionForArea(screenArea, selectMode);
            }
        }

        public ICommand StartDragCommand()
        {
            foreach (var s in _selectionHolders)
            {
                s.StartDragCommand();
            }

            return null;
        }

        public void UpdateDragCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragCommand(dt, dv);
            }
        }

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragAtStartPointCommand(dt, dv);
            }
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragAtEndPointCommand(dt, dv);
            }
        }

        public void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var s in _selectionHolders)
            {
                s.UpdateDragStretchCommand(scaleU, scaleV, originU, originV);
            }
        }

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = new TimeRange(float.PositiveInfinity, float.NegativeInfinity);

            foreach (var sh in _selectionHolders)
            {
                timeRange.Unite(sh.GetSelectionTimeRange());
            }

            return timeRange;
        }

        public void CompleteDragCommand()
        {
            foreach (var s in _selectionHolders)
            {
                s.CompleteDragCommand();
            }
        }

        public void DeleteSelectedElements()
        {
            foreach (var s in _selectionHolders)
            {
                s.DeleteSelectedElements();
            }
        }

        protected readonly List<ITimeObjectManipulation> _selectionHolders = new List<ITimeObjectManipulation>();
        #endregion
        
        protected readonly ValueSnapHandler _snapHandler = new ValueSnapHandler();

        protected ImDrawListPtr _drawlist;
        private double _lastSnapTime;
        private float _snapIndicatorDuration = 1;
        private float _lastSnapU;
    }
}