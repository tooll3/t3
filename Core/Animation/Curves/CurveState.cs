using System.Collections.Generic;

namespace T3.Core.Animation.Curves
{
    public class CurveState
    {
        public SortedList<double, VDefinition> Table { get; set; }

        public Utils.OutsideCurveBehavior PreCurveMapping
        {
            get => _preCurveMapping;
            set
            {
                _preCurveMapping = value;
                PreCurveMapper = Utils.CreateOutsideCurveMapper(value);
            }
        }
        public Utils.OutsideCurveBehavior PostCurveMapping
        {
            get => _postCurveMapping;
            set
            {
                _postCurveMapping = value;
                PostCurveMapper = Utils.CreateOutsideCurveMapper(value);
            }
        }

        public IOutsideCurveMapper PreCurveMapper { get; private set; }
        public IOutsideCurveMapper PostCurveMapper { get; private set; }

        public CurveState()
        {
            Table = new SortedList<double, VDefinition>();
            PreCurveMapping = Utils.OutsideCurveBehavior.Constant;
            PostCurveMapping = Utils.OutsideCurveBehavior.Constant;
        }

        public CurveState Clone()
        {
            var clone = new CurveState();
            clone.PreCurveMapping = _preCurveMapping;
            clone.PostCurveMapping = _postCurveMapping;

            foreach (var point in Table)
                clone.Table[point.Key] = point.Value.Clone();

            return clone;
        }

        private Utils.OutsideCurveBehavior _preCurveMapping;
        private Utils.OutsideCurveBehavior _postCurveMapping;
    }
}