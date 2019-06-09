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
    /// A graphical representation of a <see cref="_curve"/>. Handles style and selection states.
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

            int steps = (int)(ImGui.GetWindowWidth());

            //Vector2[] polyLinePointArray = new Vector2[steps];

            Vector2 lastPoint = Vector2.Zero;
            Vector2 point;
            for (int x = 0; x < steps; x++)
            {
                double u = _curveEditor.xToU(x);
                double v = _curve.GetSampledValue(u);
                float y = (float)_curveEditor.vToY(v);
                point = new Vector2((float)x, (float)y) + _curveEditor.WindowPos;
                if (x > 0)
                    _curveEditor.DrawList.AddLine(lastPoint, point, Color.White);
                lastPoint = point;

                //polyLinePointArray[i] = new Vector2(i * SAMPLE_STEP, (float)_curveEditor.vToY(v));
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
