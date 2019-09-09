using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;

namespace T3.Core.Animation.Curves
{
    public class Curve
    {
        public Utils.OutsideCurveBehavior PreCurveMapping
        {
            get => _state.PreCurveMapping;
            set => _state.PreCurveMapping = value;
        }

        public Utils.OutsideCurveBehavior PostCurveMapping
        {
            get => _state.PostCurveMapping;
            set => _state.PostCurveMapping = value;
        }

        public bool HasVAt(double u)
        {
            return _state.Table.ContainsKey(u);
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
            var foundEl = _state.Table.FirstOrDefault();
            return foundEl.Value != null && foundEl.Key < u;
        }

        public bool ExistVAfter(double u)
        {
            var foundEl = _state.Table.LastOrDefault();
            return foundEl.Value != null && foundEl.Key > u;
        }

        public double? GetPreviousU(double u)
        {
            var foundEl = _state.Table.LastOrDefault(e => e.Key < u);
            if (foundEl.Value != null)
                return foundEl.Key;
            return null;
        }

        public double? GetNextU(double u)
        {
            var foundEl = _state.Table.FirstOrDefault(e => e.Key > u);
            if (foundEl.Value != null)
                return foundEl.Key;
            return null;
        }

        public void AddOrUpdateV(double u, VDefinition key)
        {
            key.U = u;
            _state.Table[u] = key;
            SplineInterpolator.UpdateTangents(_state.Table.ToList());
        }

        public void RemoveV(double u)
        {
            var state = _state;
            state.Table.Remove(u);
            SplineInterpolator.UpdateTangents(state.Table.ToList());
        }


        /// <summary>
        /// Tries to move a keyframe to a new position
        /// </summary>
        /// <returns>Returns false if the position is already taken by a keyframe</returns>
        public bool MoveKey(double u, double newU)
        {
            var state = _state;
            if (!state.Table.ContainsKey(u))
            {
                Log.Warning("Tried to move a non-existing keyframe from {0} to {1}", u, newU);
                return false;
            }

            if (state.Table.ContainsKey(newU))
            {
                return false;
            }

            var key = state.Table[u];
            state.Table.Remove(u);
            state.Table[newU] = key;
            key.U = newU;
            SplineInterpolator.UpdateTangents(state.Table.ToList());
            return true;
        }

        public List<KeyValuePair<double, VDefinition>> GetPoints()
        {
            var points = new List<KeyValuePair<double, VDefinition>>();
            foreach (var item in _state.Table)
            {
                //points.Add(new KeyValuePair<double, VDefinition>(item.Key, item.Value.Clone()));
                points.Add(new KeyValuePair<double, VDefinition>(item.Key, item.Value));
            }
            return points;
        }

        // Returns null if there is no vDefition at that position
        public VDefinition GetV(double u)
        {
            if (_state.Table.TryGetValue(u, out var foundValue))
                return foundValue.Clone();

            return null;
        }

        public double GetSampledValue(double u)
        {
            if (_state.Table.Count < 1 || double.IsNaN(u) || double.IsInfinity(u))
                return 0.0;

            double offset = 0.0;
            double mappedU = u;
            var first = _state.Table.First();
            var last = _state.Table.Last();

            if (u <= first.Key)
            {
                _state.PreCurveMapper.Calc(u, _state.Table, out mappedU, out offset);
            }
            else if (u >= last.Key)
            {
                _state.PostCurveMapper.Calc(u, _state.Table, out mappedU, out offset);
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
                var a = _state.Table.Last(e => e.Key <= mappedU);
                var b = _state.Table.First(e => e.Key > mappedU);

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

        public virtual void Write(JsonTextWriter writer)
        {
            _state.Write(writer);
        }

        public virtual void Read(JToken inputToken)
        {
            _state.Read(inputToken);
        }

        private readonly CurveState _state = new CurveState();
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