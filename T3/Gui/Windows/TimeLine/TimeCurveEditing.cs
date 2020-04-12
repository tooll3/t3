using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using T3.Core.Animation;
using T3.Gui.Graph;

namespace T3.Gui.Windows.TimeLine
{
    public abstract class TimeCurveEditing: CurveEditing
    {
        protected List<GraphWindow.AnimationParameter> AnimationParameters;
        protected TimeLineCanvas TimeLineCanvas;

        protected override IEnumerable<Curve> GetAllCurves()
        {
            return AnimationParameters.SelectMany(param => param.Curves);
        }

        protected override void DeleteSelectedKeyframes()
        {
            TimeLineCanvas.DeleteSelectedElements();
        }

        public TimeRange GetSelectionTimeRange()
        {
            var timeRange = TimeRange.Undefined;
            foreach (var s in SelectedKeyframes)
            {
                timeRange.Unite((float)s.U);
            }

            return timeRange;
        }
        
        public void UpdateDragStretchCommand(double scaleU, double scaleV, double originU, double originV)
        {
            foreach (var vDefinition in SelectedKeyframes)
            {
                vDefinition.U = originU + (vDefinition.U - originU) * scaleU;
            }

            RebuildCurveTables();
        }
        
        
        protected override void ViewAllOrSelectedKeys(bool alsoChangeTimeRange = false)
        {
            const float curveValuePadding = 0.3f;
            const float curveTimePadding = 0.1f;

            var minU = double.PositiveInfinity;
            var maxU = double.NegativeInfinity;
            var minV = double.PositiveInfinity;
            var maxV = double.NegativeInfinity;
            var numPoints = 0;

            switch (SelectedKeyframes.Count)
            {
                case 0:
                {
                    foreach (var vDef in GetAllKeyframes())
                    {
                        numPoints++;
                        minU = Math.Min(minU, vDef.U);
                        maxU = Math.Max(maxU, vDef.U);
                        minV = Math.Min(minV, vDef.Value);
                        maxV = Math.Max(maxV, vDef.Value);
                    }

                    break;
                }
                case 1:
                    return;

                default:
                {
                    foreach (var element in SelectedKeyframes)
                    {
                        numPoints++;
                        minU = Math.Min(minU, element.U);
                        maxU = Math.Max(maxU, element.U);
                        minV = Math.Min(minV, element.Value);
                        maxV = Math.Max(maxV, element.Value);
                    }

                    break;
                }
            }

            if (numPoints == 0)
            {
                minV = -3;
                maxV = +3;
                minU = -2;
                maxU = 10;
            }

            if (Math.Abs(maxU - minU) < 0.001f)
            {
                minU -= 1;
            }

            if (Math.Abs(maxV - minU) < 0.001f)
            {
                maxV += -1;
                minV -= 1;
            }

            if (alsoChangeTimeRange)
            {
                var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
                var scaleX = (float)(size.X / ((maxU - minU) * (1 + 2 * curveTimePadding)));
                var scaleY = -(float)(size.Y / ((maxV - minV) * (1 + 2 * curveValuePadding)));
                TimeLineCanvas.Current.SetVisibleURange(
                                                       scale: new Vector2(scaleX, scaleY),
                                                       scroll: new Vector2(
                                                                           (float)minU - 150 / scaleX,
                                                                           (float)maxV - 20 / scaleY
                                                                          )
                                                      );
            }
            else
            {
                var height = ImGui.GetWindowContentRegionMax().Y - ImGui.GetWindowContentRegionMin().Y;
                var scale = -(float)(height / ((maxV - minV) * (1 + 2 * curveValuePadding)));
                TimeLineCanvas.Current.SetVisibleVRange(scale, (float)maxV - 20 / scale);
            }
        }
        
        // private IEnumerable<Curve> ParameterCurves()
        // {
        //     return AnimationParameters.SelectMany(param => param.Curves);
        // }
    }
}