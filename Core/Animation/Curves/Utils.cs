using System.Linq;

namespace T3.Core.Animation.Curves
{
    public static class Utils
    {
        public enum OutsideCurveBehavior
        {
            Constant = 0,
            Cycle,
            CycleWithOffset,
            Oscillate
        };

        static public IOutsideCurveMapper CreateOutsideCurveMapper(OutsideCurveBehavior outsideBehavior)
        {
            switch (outsideBehavior)
            {
                case OutsideCurveBehavior.Constant: return new ConstantCurveMapper();
                case OutsideCurveBehavior.Cycle: return new CycleCurveMapper();
                case OutsideCurveBehavior.CycleWithOffset: return new CycleWithOffsetCurveMapper();
                case OutsideCurveBehavior.Oscillate: return new OscillateCurveMapper();
            }
            throw new System.Exception("undefined outside behavior");
        }

        //internal static void AddKeyframeAtTime(ICurve curve, double time, double value)
        //{
        //    var newKey = new VDefinition();

        //    double? prevU = curve.GetPreviousU(time);
        //    if (prevU != null)
        //        newKey = curve.GetV(prevU.Value).Clone();

        //    newKey.Value = value;

        //    curve.AddOrUpdateV(time, newKey);
        //}

        //internal static void AddKeyframeAtTime(OperatorPart opPart, ICurve curve, double time)
        //{
        //    AddKeyframeAtTime(curve, time, GetCurrentValueAtTime(opPart, time));
        //}

        //public static float GetCurrentValueAtTime(OperatorPart opPart, double time)
        //{
        //    OperatorPartContext context = new OperatorPartContext() { Time = (float)time };
        //    return opPart.Eval(context).Value;
        //}

        //public static OperatorPart GetOperatorPartBelongingToCurve(ICurve curve)
        //{
        //    var curveFunc = curve as OperatorPart.Function;
        //    var compositionOp = curveFunc.OperatorPart.Parent.Parent;
        //    var animatedOpPart = (from connection in compositionOp.Connections
        //                          where connection.SourceOp == curveFunc.OperatorPart.Parent
        //                          select connection.TargetOpPart).SingleOrDefault();

        //    return animatedOpPart;
        //}
    }
}
