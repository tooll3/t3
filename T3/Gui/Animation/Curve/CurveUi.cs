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
        public List<CurvePointUi> CurvePoints { get; set; }

        public CurveUi(Curve curve, CurveEditor curveEditor)
        {
            _curveEditor = curveEditor;
            _curve = curve;

            CurvePoints = new List<CurvePointUi>();
            foreach (var pair in curve.GetPoints())
            {
                var key = pair.Value;
                CurvePoints.Add(new CurvePointUi(key, curve, curveEditor));
            }
        }


        public void Draw()
        {
            foreach (var p in CurvePoints)
            {
                p.Draw();
            }
            DrawLine();
        }


        private void DrawLine()
        {
            var step = 3f;
            var width = (float)ImGui.GetWindowWidth();

            double dU = _curveEditor.xToU((double)step) - _curveEditor.xToU(0);
            double u = _curveEditor.xToU(1);
            float x = 0;

            var steps = (int)(width / step);
            if (_points.Length != steps)
            {
                _points = new Vector2[steps];
            }

            for (int i = 0; i < steps; i++)
            {
                _points[i] = new Vector2(
                    x,
                    _curveEditor.vToY((float)_curve.GetSampledValue(u))
                    ) + _curveEditor.WindowPos;

                u += dU;
                x += step;
            }
            _curveEditor.DrawList.AddPolyline(ref _points[0], steps, Color.Gray, false, 1);
        }

        private Curve _curve;
        private static Vector2[] _points = new Vector2[2];
        private CurveEditor _curveEditor;
    }
}
