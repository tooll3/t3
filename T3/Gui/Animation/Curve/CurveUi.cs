using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Animation.Curve;

namespace T3.Gui.Animation
{
    /// <summary>
    /// A graphical representation of a <see cref="Curve"/>. Handles style and selection states.
    /// </summary>
    public class CurveUi
    {
        public bool IsHighlighted { get; set; }
        private CurveEditor _curveEditor;
        public List<CurvePointUi> CurvePoints { get; set; }

        public CurveUi(Curve curve, CurveEditor curveEditor)
        {
            _curveEditor = curveEditor;
            _curve = curve;

            CurvePoints = new List<CurvePointUi>();
            foreach (var pair in curve.GetPoints())
            {
                var key = pair.Value;
                //var u = pair.Key;
                CurvePoints.Add(new CurvePointUi(key, curve, curveEditor));
            }
        }


        public Curve _curve;

        public void Draw()
        {
            foreach (var p in CurvePoints)
            {
                p.Draw();
            }
            DrawLine();
        }


        public void DrawLine()
        {
            //m_Stopwatch.Restart();

            //foreach (var cpc in _curvesWithPointControls[curve])
            //{
            //    cpc.UpdateControlTangents();
            //}
            ////m_Stopwatch.Stop();
            //m_Stopwatch.Restart();

            //var path = _curvesWithPaths[curve];

            //PathGeometry myPathGeometry = new PathGeometry();
            //PathFigure pathFigure2 = new PathFigure();
            var step = 3f;
            var width = (float)ImGui.GetWindowWidth();

            Vector2 lastPoint = Vector2.Zero;
            Vector2 point;
            double dU = _curveEditor.xToU((double)step) - _curveEditor.xToU(0);
            double u = _curveEditor.xToU(1);

            for (float x = 0; x < width; x += step)
            {
                point = new Vector2(
                    x,
                    _curveEditor.vToY((float)_curve.GetSampledValue(u))
                    ) + _curveEditor.WindowPos;

                if (x > 0)
                    _curveEditor.DrawList.AddLine(lastPoint, point, Color.Gray);

                u += dU;
                lastPoint = point;
            }

            //if (steps == 0)
            //{
            //    return;
            //}



            //pathFigure2.StartPoint = polyLinePointArray[0];
            //PolyLineSegment myPolyLineSegment = new PolyLineSegment();

            //myPolyLineSegment.Points = new PointCollection(polyLinePointArray);
            //pathFigure2.Segments.Add(myPolyLineSegment);
            //myPathGeometry.Figures.Add(pathFigure2);
            //path.Data = myPathGeometry;
            //m_Stopwatch.Stop();
        }
    }
}
