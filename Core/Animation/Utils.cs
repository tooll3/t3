namespace T3.Core.Animation;

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
            case OutsideCurveBehavior.Constant:        return new ConstantCurveMapper();
            case OutsideCurveBehavior.Cycle:           return new CycleCurveMapper();
            case OutsideCurveBehavior.CycleWithOffset: return new CycleWithOffsetCurveMapper();
            case OutsideCurveBehavior.Oscillate:       return new OscillateCurveMapper();
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
}