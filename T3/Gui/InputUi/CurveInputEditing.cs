using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Commands;
using T3.Gui.Interaction;
using T3.Gui.Interaction.WithCurves;
using T3.Gui.Windows.TimeLine;

namespace T3.Gui.InputUi
{
    /// <summary>
    /// Handles editing of Curve-Inputs in parameter window.
    /// </summary>
    public static class CurveInputEditing
    {
        private static readonly Dictionary<Curve, CurveInteraction> InteractionForCurve = new Dictionary<Curve, CurveInteraction>();

        public static void DrawCanvasForCurve(Curve curve)
        {
            if (!InteractionForCurve.TryGetValue(curve, out var curveInteraction))
            {
                curveInteraction = new CurveInteraction()
                                       {
                                           Curves = new List<Curve>() { curve }
                                       };

                InteractionForCurve.Add(curve, curveInteraction);
            }

            curveInteraction.Draw();
        }

        /// <summary>
        /// Implement interaction of manipulating the individual keyframes
        /// </summary>
        private class CurveInteraction : CurveEditing
        {
            public List<Curve> Curves = new List<Curve>();
            private readonly SingleCurveEditCanvas _canvas = new SingleCurveEditCanvas() { ImGuiTitle = "canvas" + InteractionForCurve.Count };

            public void Draw()
            {
                _canvas.Draw(Curves[0], this);
            }

            protected override IEnumerable<Curve> GetAllCurves()
            {
                return Curves;
            }

            protected override void ViewAllOrSelectedKeys(bool alsoChangeTimeRange = false)
            {
            }

            protected override void DeleteSelectedKeyframes()
            {
            }

            protected internal override void HandleCurvePointDragging(VDefinition vDef, bool isSelected)
            {
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                }

                if (!ImGui.IsItemActive() || !ImGui.IsMouseDragging(0, 0f))
                    return;

                if (ImGui.GetIO().KeyCtrl)
                {
                    if (isSelected)
                        SelectedKeyframes.Remove(vDef);

                    return;
                }

                if (!isSelected)
                {
                    if (!ImGui.GetIO().KeyShift)
                    {
                        _canvas.ClearSelection();
                    }

                    SelectedKeyframes.Add(vDef);
                }

                if (_changeKeyframesCommand == null)
                {
                    _canvas.StartDragCommand();
                }

                var newDragPosition = _canvas.InverseTransformPosition(ImGui.GetIO().MousePos);
                double u = newDragPosition.X;
                _canvas.SnapHandlerForU.CheckForSnapping(ref u, _canvas.Scale.X);

                double v = newDragPosition.Y;
                _canvas.SnapHandlerForV.CheckForSnapping(ref v, _canvas.Scale.Y);

                UpdateDragCommand(u - vDef.U, v - vDef.Value);
            }

            public ICommand StartDragCommand()
            {
                _changeKeyframesCommand = new ChangeKeyframesCommand(Guid.Empty, SelectedKeyframes);
                return _changeKeyframesCommand;
            }

            public void UpdateDragCommand(double dt, double dv)
            {
                foreach (var vDefinition in SelectedKeyframes)
                {
                    vDefinition.U += dt;
                    vDefinition.Value += dv;
                }

                RebuildCurveTables();
            }

            public void CompleteDragCommand()
            {
                if (_changeKeyframesCommand == null)
                    return;

                _changeKeyframesCommand.StoreCurrentValues();
                UndoRedoStack.Add(_changeKeyframesCommand);
                _changeKeyframesCommand = null;
            }

            private static ChangeKeyframesCommand _changeKeyframesCommand;

            private void HandleFenceSelection()
            {
                _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
                switch (_fenceState)
                {
                    case SelectionFence.States.Updated:
                        var boundsInCanvas = _canvas.InverseTransformRect(SelectionFence.BoundsInScreen).MakePositive();
                        SelectedKeyframes.Clear();
                        foreach (var point in GetAllKeyframes())
                        {
                            if (boundsInCanvas.Contains(new Vector2((float)point.U, (float)point.Value)))
                                SelectedKeyframes.Add(point);
                        }

                        break;

                    case SelectionFence.States.CompletedAsClick:
                        SelectedKeyframes.Clear();
                        break;
                }
            }

            private SelectionFence.States _fenceState = SelectionFence.States.Inactive;

            /// <summary>
            /// Implement canvas for showing and manipulating curve
            /// </summary>
            private class SingleCurveEditCanvas : CurveEditCanvas
            {
                public SingleCurveEditCanvas()
                {
                    SnapHandlerForU.AddSnapAttractor(_standardRaster);
                    SnapHandlerForV.AddSnapAttractor(_horizontalRaster);
                }

                public void Draw(Curve curve, CurveInteraction interaction)
                {
                    DrawCurveCanvas(DrawCanvasContent, height: 150);

                    void DrawCanvasContent()
                    {
                        _standardRaster.Draw(this);
                        _horizontalRaster.Draw(this);
                        TimelineCurveEditArea.DrawCurveLine(curve, this);

                        foreach (var keyframe in interaction.GetAllKeyframes().ToArray())
                        {
                            CurvePoint.Draw(keyframe, this, interaction.SelectedKeyframes.Contains(keyframe), interaction);
                        }
                        
                        interaction.HandleFenceSelection();
                        interaction.DrawContextMenu();
                        HandleCreateNewKeyframes(curve);
                        if (_needToAdjustScopeAfterFirstRendering)
                        {
                            var bounds = GetBoundsOnCanvas(interaction.GetAllKeyframes());
                            SetScopeToCanvasArea(bounds, flipY: true);
                            _needToAdjustScopeAfterFirstRendering = false;
                        }
                    }
                }

                private readonly StandardTimeRaster _standardRaster = new StandardTimeRaster();
                private readonly HorizontalRaster _horizontalRaster = new HorizontalRaster();
                private bool _needToAdjustScopeAfterFirstRendering = true;
            }
        }
    }
}