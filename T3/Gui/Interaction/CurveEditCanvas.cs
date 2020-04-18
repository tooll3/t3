using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Interaction.Snapping;
using T3.Gui.Windows.TimeLine;
using UiHelpers;
using System;
using T3.Gui.Commands;

namespace T3.Gui.Interaction
{
    public abstract class CurveEditCanvas: ScalableCanvas, ITimeObjectManipulation
    {
        protected CurveEditCanvas()
        {
            ScrollTarget = new Vector2(500f, 0.0f);
            ScaleTarget = new Vector2(80, -1);

            SnapHandler.SnappedEvent += SnappedEventHandler;
        }

        public string ImGuiTitle = "timeline";

        protected void DrawCurveCanvas(Action drawAdditionalCanvasContent, float height=0)
        {
            Drawlist = ImGui.GetWindowDrawList();
            
            ImGui.BeginChild(ImGuiTitle, new Vector2(0, height), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoMove);
            {
                UpdateCanvas();
                Drawlist = ImGui.GetWindowDrawList();

                drawAdditionalCanvasContent();
                HandleFenceUpdate();
                DrawSnapIndicator();
            }
            ImGui.EndChild();
        }

        private void HandleFenceUpdate()
        {
            _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
            switch (_fenceState)
            {
                case SelectionFence.States.Updated:
                    this.UpdateSelectionForArea(SelectionFence.BoundsInScreen, SelectionFence.SelectMode);
                    break;
            }
        }

        private SelectionFence.States _fenceState;
        
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
            Drawlist.AddRectFilled(p, p + new Vector2(1, 2000), color);
        }
        
        
        #region implement ITimeObjectManipulation to forward interaction to children
        public void ClearSelection()
        {
            foreach (var sh in TimeObjectManipulators)
            {
                sh.ClearSelection();
            }
        }

        public void UpdateSelectionForArea(ImRect screenArea, SelectionFence.SelectModes selectMode)
        {
            foreach (var sh in TimeObjectManipulators)
            {
                sh.UpdateSelectionForArea(screenArea, selectMode);
            }
        }

        public ICommand StartDragCommand()
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.StartDragCommand();
            }

            return null;
        }

        public void UpdateDragCommand(double dt, double dv)
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.UpdateDragCommand(dt, dv);
            }
        }

        public void UpdateDragAtStartPointCommand(double dt, double dv)
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.UpdateDragAtStartPointCommand(dt, dv);
            }
        }

        public void UpdateDragAtEndPointCommand(double dt, double dv)
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.UpdateDragAtEndPointCommand(dt, dv);
            }
        }

        public void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.UpdateDragStretchCommand(scaleU, scaleV, originU, originV);
            }
        }

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = new TimeRange(float.PositiveInfinity, float.NegativeInfinity);

            foreach (var sh in TimeObjectManipulators)
            {
                timeRange.Unite(sh.GetSelectionTimeRange());
            }

            return timeRange;
        }

        public void CompleteDragCommand()
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.CompleteDragCommand();
            }
        }

        public void DeleteSelectedElements()
        {
            foreach (var s in TimeObjectManipulators)
            {
                s.DeleteSelectedElements();
            }
        }

        protected readonly List<ITimeObjectManipulation> TimeObjectManipulators = new List<ITimeObjectManipulation>();
        #endregion
        
        protected readonly ValueSnapHandler SnapHandler = new ValueSnapHandler();
        protected ImDrawListPtr Drawlist;
        private double _lastSnapTime;
        private float _snapIndicatorDuration = 1;
        private float _lastSnapU;
    }
}