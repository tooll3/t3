using System.Numerics;
using T3.Gui.Interaction;

namespace T3.Gui.Windows.TimeLine
{
    public abstract class CurveCanvas : ScalableCanvas
    {
        public CurveCanvas()
        {
            ScrollTarget = new Vector2(500f, 0.0f);
            ScaleTarget = new Vector2(80, -1);
        }
    }
}