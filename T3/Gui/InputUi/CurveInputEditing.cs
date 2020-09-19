using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Gui.Commands;
using T3.Gui.Graph;
using T3.Gui.Interaction;
using T3.Gui.Interaction.WithCurves;
using T3.Gui.Windows.TimeLine;

namespace T3.Gui.InputUi
{
    /// <summary>
    /// Handles editing of Curve-Inputs in parameter windows and Graph CustomUi.
    /// </summary>
    /// <remarks>
    /// The view settings (selection, zoom, scale, etc) for each canvas are stored in a dictionary of <see cref="CurveInteraction"/> instances.  
    /// </remarks>
    public static class CurveInputEditing
    {
        public static InputEditStateFlags DrawCanvasForCurve(Curve curve, T3Ui.EditingFlags flags = T3Ui.EditingFlags.None)
        {
            //Log.Debug("ID " + ImGui.GetID("") );
            var imGuiId = ImGui.GetID("");
            _flags = flags;
            if (!InteractionForCurve.TryGetValue(imGuiId, out var curveInteraction))
            {
                curveInteraction = new CurveInteraction()
                                       {
                                           Curves = new List<Curve>() { curve }
                                       };

                InteractionForCurve.Add(imGuiId, curveInteraction);
            }

            curveInteraction.EditState = InputEditStateFlags.Nothing;
            curveInteraction.Draw();

            return curveInteraction.EditState;
        }

        public static ScalableCanvas GetCanvasForCurve(Curve curve)
        {
            return null;

            // if (!InteractionForCurve.TryGetValue(curve, out var curveInteraction))
            //     return null;
            //
            // return curveInteraction.Canvas;
        }

        /// <summary>
        /// Implement interaction of manipulating the individual keyframes
        /// </summary>
        private class CurveInteraction : CurveEditing
        {
            public List<Curve> Curves = new List<Curve>();
            private readonly SingleCurveEditCanvas _singleCurveCanvas = new SingleCurveEditCanvas() { ImGuiTitle = "canvas" + InteractionForCurve.Count };

            //public ScalableCanvas Canvas => _canvas;

            public InputEditStateFlags EditState { get; set; } = InputEditStateFlags.Nothing;

            public void Draw()
            {
                _singleCurveCanvas.Draw(Curves[0], this);
            }

            #region implement editing ---------------------------------------------------------------
            protected override IEnumerable<Curve> GetAllCurves()
            {
                return Curves;
            }

            protected override void ViewAllOrSelectedKeys(bool alsoChangeTimeRange = false)
            {
                _singleCurveCanvas.NeedToAdjustScopeAfterFirstRendering = true;
            }

            protected override void DeleteSelectedKeyframes()
            {
                foreach (var curve in GetAllCurves())
                {
                    foreach (var keyframe in curve.GetVDefinitions().ToList())
                    {
                        if (!SelectedKeyframes.Contains(keyframe))
                            continue;

                        curve.RemoveKeyframeAt(keyframe.U);
                        SelectedKeyframes.Remove(keyframe);
                    }
                }

                EditState = InputEditStateFlags.Modified;
            }

            protected internal override void HandleCurvePointDragging(VDefinition vDef, bool isSelected)
            {
                if ((_flags & T3Ui.EditingFlags.PreventMouseInteractions) != 0)
                    return;

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);
                }



                if (!ImGui.IsItemActive())
                {
                    if (ImGui.IsItemDeactivated())
                    {
                        if (_changeKeyframesCommand != null)
                        {
                            CompleteDragCommand();
                        }
                        else
                        {
                            Log.Error("Deactivated keyframe dragging without valid command?");
                        }
                    }
                    return;
                }

                // Sadly, this hotkey interferes with the "Allow manipulation in graph custom ui hot key"
                // if (ImGui.GetIO().KeyCtrl)
                // {
                //     if (isSelected)
                //         SelectedKeyframes.Remove(vDef);
                //
                //     return;
                // }

                if (!isSelected)
                {
                    if (!ImGui.GetIO().KeyShift)
                    {
                        SelectedKeyframes.Clear();
                    }

                    SelectedKeyframes.Add(vDef);
                }

                if (!ImGui.IsMouseDragging(0, 2f))
                    return;

                if (_changeKeyframesCommand == null)
                {
                    StartDragCommand();
                }

                var newDragPosition = _singleCurveCanvas.InverseTransformPosition(ImGui.GetIO().MousePos);
                double u = newDragPosition.X;
                _singleCurveCanvas.SnapHandlerForU.CheckForSnapping(ref u, _singleCurveCanvas.Scale.X);

                double v = newDragPosition.Y;
                _singleCurveCanvas.SnapHandlerForV.CheckForSnapping(ref v, _singleCurveCanvas.Scale.Y);

                UpdateDragCommand(u - vDef.U, v - vDef.Value);

                EditState = InputEditStateFlags.Modified;
            }

            private ICommand StartDragCommand()
            {
                _changeKeyframesCommand = new ChangeKeyframesCommand(Guid.Empty, SelectedKeyframes);
                return _changeKeyframesCommand;
            }

            private void UpdateDragCommand(double dt, double dv)
            {
                foreach (var vDefinition in SelectedKeyframes)
                {
                    vDefinition.U += dt;
                    vDefinition.Value += dv;
                }

                RebuildCurveTables();
                EditState = InputEditStateFlags.Modified;
            }
            

            // FIXME: This needs to be called
            public void CompleteDragCommand()
            {
                if (_changeKeyframesCommand == null)
                    return;

                _changeKeyframesCommand.StoreCurrentValues();
                UndoRedoStack.Add(_changeKeyframesCommand);
                _changeKeyframesCommand = null;
                EditState = InputEditStateFlags.Finished;
            }

            private static ChangeKeyframesCommand _changeKeyframesCommand;
            #endregion

            #region handle selection ----------------------------------------------------------------
            
            private void HandleFenceSelection()
            {
                _fenceState = SelectionFence.UpdateAndDraw(_fenceState);
                switch (_fenceState)
                {
                    case SelectionFence.States.Updated:
                        var boundsInCanvas = _singleCurveCanvas.InverseTransformRect(SelectionFence.BoundsInScreen).MakePositive();
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
            #endregion

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
                    var height = (_flags & T3Ui.EditingFlags.ExpandVertically) == T3Ui.EditingFlags.ExpandVertically
                                     ? ImGui.GetContentRegionAvail().Y
                                     : DefaultCurveParameterHeight;
                    
                    var preventZoomWithMouseWheel = ImGui.GetIO().KeyCtrl ? T3Ui.EditingFlags.None 
                                                        : T3Ui.EditingFlags.PreventZoomWithMouseWheel | T3Ui.EditingFlags.PreventPanningWithMouse;
                    DrawCurveCanvas(DrawCanvasContent, height, preventZoomWithMouseWheel);

                    void DrawCanvasContent()
                    {
                        _standardRaster.Draw(this);
                        _horizontalRaster.Draw(this);
                        curve.UpdateTangents();
                        TimelineCurveEditArea.DrawCurveLine(curve, this);

                        foreach (var keyframe in interaction.GetAllKeyframes().ToArray())
                        {
                            CurvePoint.Draw(keyframe, this, interaction.SelectedKeyframes.Contains(keyframe), interaction);
                        }

                        interaction.HandleFenceSelection();

                        // Handle keyboard interaction 
                        if (ImGui.IsWindowHovered() && KeyboardBinding.Triggered(UserActions.FocusSelection))
                        {
                            interaction.ViewAllOrSelectedKeys();
                        }

                        if (ImGui.IsWindowHovered() && KeyboardBinding.Triggered(UserActions.DeleteSelection))
                        {
                            interaction.DeleteSelectedKeyframes();
                        }

                        interaction.DrawContextMenu();
                        HandleCreateNewKeyframes(curve);
                        if (NeedToAdjustScopeAfterFirstRendering)
                        {
                            var bounds = GetBoundsOnCanvas(interaction.GetAllKeyframes());
                            SetScopeToCanvasArea(bounds, flipY: true, GraphCanvas.Current);
                            NeedToAdjustScopeAfterFirstRendering = false;
                        }
                    }
                }

                private const float DefaultCurveParameterHeight = 100;
                private readonly StandardValueRaster _standardRaster = new StandardValueRaster() { EnableSnapping = true };
                private readonly HorizontalRaster _horizontalRaster = new HorizontalRaster();
                public bool NeedToAdjustScopeAfterFirstRendering = true;
            }
        }

        private static readonly Dictionary<uint, CurveInteraction> InteractionForCurve = new Dictionary<uint, CurveInteraction>();


        private static T3Ui.EditingFlags _flags;

        public enum MoveDirections
        {
            Undecided = 0,
            Vertical,
            Horizontal,
            Both
        }

        public static MoveDirections MoveDirection = MoveDirections.Undecided;
        public const float MoveDirectionThreshold = 2;
    }
}