using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;

namespace T3.Core.Animation.Curve
{
    public abstract class Curve
    {
        public const int CURVE_U_PRECISION_DIGITS = 6;

        public Utils.OutsideCurveBehavior PreCurveMapping
        {
            get { return State.PreCurveMapping; }
            set { State.PreCurveMapping = value; }
        }

        public Utils.OutsideCurveBehavior PostCurveMapping
        {
            get { return State.PostCurveMapping; }
            set { State.PostCurveMapping = value; }
        }

        public bool ChangedEventEnabled { get; set; }

        public int ComponentIndex { get; set; }

        public bool HasVAt(double u)
        {
            return State.Table.ContainsKey(u);
        }

        public int CompareTo(object obj)
        {
            if (obj.GetHashCode() < GetHashCode())
            {
                return -1;
            }
            else if (obj.GetHashCode() > GetHashCode())
            {
                return 1;
            }
            return 0;
        }

        public bool ExistVBefore(double u)
        {
            var foundEl = State.Table.FirstOrDefault();
            return foundEl.Value != null && foundEl.Key < u;
        }

        public bool ExistVAfter(double u)
        {
            var foundEl = State.Table.LastOrDefault();
            return foundEl.Value != null && foundEl.Key > u;
        }

        public double? GetPreviousU(double u)
        {
            var foundEl = State.Table.LastOrDefault(e => e.Key < u);
            if (foundEl.Value != null)
                return foundEl.Key;
            return null;
        }

        public double? GetNextU(double u)
        {
            var foundEl = State.Table.FirstOrDefault(e => e.Key > u);
            if (foundEl.Value != null)
                return foundEl.Key;
            return null;
        }

        public void AddOrUpdateV(double u, VDefinition key)
        {
            var state = State;
            state.Table[u] = key.Clone();
            SplineInterpolator.UpdateTangents(state.Table.ToList());

            //TriggerChangedEventIfEnabled();
        }

        public void RemoveV(double u)
        {
            var state = State;
            state.Table.Remove(u);
            SplineInterpolator.UpdateTangents(state.Table.ToList());

            //TriggerChangedEventIfEnabled();
        }



        public void MoveV(double u, double newU)
        {
            var state = State;
            if (!state.Table.ContainsKey(u))
            {
                Log.Warning("Tried to move a non-existing keyframe from {0} to {1}", u, newU);
                return;
            }
            var key = state.Table[u];
            state.Table.Remove(u);
            state.Table[newU] = key;
            SplineInterpolator.UpdateTangents(state.Table.ToList());

            //TriggerChangedEventIfEnabled();
        }

        public List<KeyValuePair<double, VDefinition>> GetPoints()
        {
            var points = new List<KeyValuePair<double, VDefinition>>();
            foreach (var item in State.Table)
                points.Add(new KeyValuePair<double, VDefinition>(item.Key, item.Value.Clone()));
            return points;
        }

        // Returns null if there is no vDefition at that position
        public VDefinition GetV(double u)
        {
            VDefinition foundValue;
            if (State.Table.TryGetValue(u, out foundValue))
                return foundValue.Clone();

            return null;
        }

        public double GetSampledValue(double u)
        {
            var state = State;
            if (state.Table.Count < 1 || double.IsNaN(u) || double.IsInfinity(u))
                return 0.0;

            double offset = 0.0;
            double mappedU = u;
            var first = state.Table.First();
            var last = state.Table.Last();

            if (u <= first.Key)
            {
                state.PreCurveMapper.Calc(u, state.Table, out mappedU, out offset);
            }
            else if (u >= last.Key)
            {
                state.PostCurveMapper.Calc(u, state.Table, out mappedU, out offset);
            }

            double resultValue = 0.0;
            if (mappedU <= first.Key)
            {
                resultValue = offset + first.Value.Value;
            }
            else if (mappedU >= last.Key)
            {
                resultValue = offset + last.Value.Value;
            }
            else
            {
                //interpolate
                var a = state.Table.Last(e => e.Key <= mappedU);
                var b = state.Table.First(e => e.Key > mappedU);

                if (a.Value.OutType == VDefinition.Interpolation.Constant)
                {
                    resultValue = offset + ConstInterpolator.Interpolate(a, b, mappedU);
                }
                else if (a.Value.OutType == VDefinition.Interpolation.Linear && b.Value.OutType == VDefinition.Interpolation.Linear)
                {
                    resultValue = offset + LinearInterpolator.Interpolate(a, b, mappedU);
                }
                else
                {
                    resultValue = offset + SplineInterpolator.Interpolate(a, b, mappedU);
                }
            }

            return resultValue;
        }

        public Curve()
        {
            ChangedEventEnabled = true;
        }

        //protected void TriggerChangedEventIfEnabled()
        //{
        //    if (ChangedEventEnabled)
        //    {
        //        //TriggerChangedEvent(new System.EventArgs());
        //    }
        //}

        protected CurveState State { get; set; }
        //{
        //    get
        //    {
        //        try
        //        {
        //            return OperatorPart.Parent.GetOperatorPartState(OperatorPart.ID) as CurveState;
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("could not get the CurveState for this Curve op: {0}", ex.Message);
        //        }
        //        return null;
        //    }
        //}
    }

    /// <summary>
    /// The keyframe helper class is used for serializing a list of keyframes to JSON
    /// </summary>
    public class Keyframe
    {
        public double Time { get; set; }
        public VDefinition VDefinition { get; set; }
    }

}