using System.Collections.Generic;
using System.Numerics;
using T3.Core.Animation;
using T3.Gui.Animation.CurveEditing;
using T3.Gui.Interaction;
using T3.Gui.Windows.TimeLine;

namespace T3.Gui.InputUi
{
    /// <summary>
    /// A static class that helps to draw and edit parameters for curve inputs
    /// in parameter Window.
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

        
        private class CurveInteraction : CurveEditing
        {
            public List<Curve> Curves = new List<Curve>();
            private readonly SingleCurveEditCanvas _canvas = new SingleCurveEditCanvas();
            
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
            
            
            private class SingleCurveEditCanvas: CurveEditCanvas
            {
                public void Draw(Curve curve, CurveInteraction interaction)
                {
                    DrawCurveCanvas(DrawCanvasContent);

                    void DrawCanvasContent()
                    {
                        StandardRaster.Draw(this);
                        HorizontalRaster.Draw(this);
                        TimelineCurveEditArea.DrawCurveLine(curve, this);

                        foreach (var keyframe in interaction.GetAllKeyframes())
                        {
                            CurvePoint.Draw(keyframe, this,  interaction.SelectedKeyframes.Contains(keyframe), null);
                        }
                        
                        interaction.HandleFenceSelection();
                    }
                }
                
                private static readonly StandardTimeRaster StandardRaster = new StandardTimeRaster();
                private static readonly HorizontalRaster HorizontalRaster = new HorizontalRaster();
            }
        }
    }
}