using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator.Slots;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Core.Operator
{
    public class SymbolExtension
    {
        // todo: how is a symbol extension defined, what exactly does this mean
    }

    public class Animator : SymbolExtension
    {
        struct CurveId
        {
            public CurveId(Guid instanceId, Guid inputId, int index = 0)
            {
                InstanceId = instanceId; // TODO: Shouldn't this be symbolChildId?
                InputId = inputId;
                Index = index;
            }

            public CurveId(IInputSlot inputSlot, int index = 0)
            {
                InstanceId = inputSlot.Parent.SymbolChildId;
                InputId = inputSlot.Id;
                Index = index;
            }

            public readonly Guid InstanceId;
            public readonly Guid InputId;
            public readonly int Index;
        }

        public void CopyAllAnimationsTo(Animator targetAnimator, Dictionary<Guid, Guid> oldToNewIdDict)
        {
            Debug.Assert(targetAnimator._animatedInputCurves.Count == 0);
            foreach (var (id, curve) in _animatedInputCurves)
            {
                CloneAndAddCurve(targetAnimator, oldToNewIdDict, id, curve);
            }
        }

        public void CopyAnimationsTo(Animator targetAnimator, List<Guid> childrenToCopyAnimationsFrom, Dictionary<Guid, Guid> oldToNewIdDict)
        {
            foreach (var (id, curve) in _animatedInputCurves)
            {
                if (!childrenToCopyAnimationsFrom.Contains(id.InstanceId))
                    continue;

                CloneAndAddCurve(targetAnimator, oldToNewIdDict, id, curve);
            }
        }

        public void RemoveAnimationsFromInstances(List<Guid> instanceIds)
        {
            List<CurveId> elementsToDelete = new List<CurveId>();
            foreach (var (id, curve) in _animatedInputCurves)
            {
                if (!instanceIds.Contains(id.InstanceId))
                    continue;

                elementsToDelete.Add(id);
            }

            foreach (var idToDelete in elementsToDelete)
            {
                _animatedInputCurves.Remove(idToDelete);
            }
        }

        private static void CloneAndAddCurve(Animator targetAnimator, Dictionary<Guid, Guid> oldToNewIdDict, CurveId id, Curve curve)
        {
            Guid newInstanceId = oldToNewIdDict[id.InstanceId];
            var newCurveId = new CurveId(newInstanceId, id.InputId, id.Index);
            var newCurve = curve.TypedClone();
            targetAnimator._animatedInputCurves.Add(newCurveId, newCurve);
        }

        public void CreateInputUpdateAction<T>(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> floatInputSlot)
            {
                var newCurve = new Curve();
                newCurve.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                              {
                                                                                  Value = floatInputSlot.Value,
                                                                                  InType = VDefinition.Interpolation.Spline,
                                                                                  OutType = VDefinition.Interpolation.Spline,
                                                                              });
                _animatedInputCurves.Add(new CurveId(inputSlot), newCurve);

                floatInputSlot.UpdateAction = context => { floatInputSlot.Value = (float)newCurve.GetSampledValue(context.TimeInBars); };
                floatInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }
            else if (inputSlot is Slot<Vector2> vector2InputSlot)
            {
                var newCurveX = new Curve();
                newCurveX.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector2InputSlot.Value.X,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 0), newCurveX);

                var newCurveY = new Curve();
                newCurveY.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector2InputSlot.Value.Y,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 1), newCurveY);

                vector2InputSlot.UpdateAction = context =>
                                                {
                                                    vector2InputSlot.Value.X = (float)newCurveX.GetSampledValue(context.TimeInBars);
                                                    vector2InputSlot.Value.Y = (float)newCurveY.GetSampledValue(context.TimeInBars);
                                                };
                vector2InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }
            else if (inputSlot is Slot<Vector3> vector3InputSlot)
            {
                var newCurveX = new Curve();
                newCurveX.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector3InputSlot.Value.X,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 0), newCurveX);

                var newCurveY = new Curve();
                newCurveY.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector3InputSlot.Value.Y,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 1), newCurveY);

                var newCurveZ = new Curve();
                newCurveZ.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector3InputSlot.Value.Z,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 2), newCurveZ);

                vector3InputSlot.UpdateAction = context =>
                                                {
                                                    vector3InputSlot.Value.X = (float)newCurveX.GetSampledValue(context.TimeInBars);
                                                    vector3InputSlot.Value.Y = (float)newCurveY.GetSampledValue(context.TimeInBars);
                                                    vector3InputSlot.Value.Z = (float)newCurveZ.GetSampledValue(context.TimeInBars);
                                                };
                vector3InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }
            else if (inputSlot is Slot<Vector4> vector4InputSlot)
            {
                var newCurveX = new Curve();
                newCurveX.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector4InputSlot.Value.X,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 0), newCurveX);

                var newCurveY = new Curve();
                newCurveY.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector4InputSlot.Value.Y,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 1), newCurveY);

                var newCurveZ = new Curve();
                newCurveZ.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector4InputSlot.Value.Z,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 2), newCurveZ);

                var newCurveW = new Curve();
                newCurveW.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = vector4InputSlot.Value.W,
                                                                                   InType = VDefinition.Interpolation.Spline,
                                                                                   OutType = VDefinition.Interpolation.Spline,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 3), newCurveW);

                vector4InputSlot.UpdateAction = context =>
                                                {
                                                    vector4InputSlot.Value.X = (float)newCurveX.GetSampledValue(context.TimeInBars);
                                                    vector4InputSlot.Value.Y = (float)newCurveY.GetSampledValue(context.TimeInBars);
                                                    vector4InputSlot.Value.Z = (float)newCurveZ.GetSampledValue(context.TimeInBars);
                                                    vector4InputSlot.Value.W = (float)newCurveW.GetSampledValue(context.TimeInBars);
                                                };
                vector4InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }
            else if (inputSlot is Slot<int> intInputSlot)
            {
                var newCurve = new Curve();
                newCurve.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                              {
                                                                                  Value = intInputSlot.Value,
                                                                                  InType = VDefinition.Interpolation.Constant,
                                                                                  OutType = VDefinition.Interpolation.Constant,
                                                                                  InEditMode = VDefinition.EditMode.Constant,
                                                                                  OutEditMode = VDefinition.EditMode.Constant,
                                                                              });
                _animatedInputCurves.Add(new CurveId(inputSlot), newCurve);

                intInputSlot.UpdateAction = context => { intInputSlot.Value = (int)newCurve.GetSampledValue(context.TimeInBars); };
                intInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }
            else if (inputSlot is Slot<Size2> size2InputSlot)
            {
                var newCurveX = new Curve();
                newCurveX.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = size2InputSlot.Value.Width,
                                                                                   InType = VDefinition.Interpolation.Constant,
                                                                                   OutType = VDefinition.Interpolation.Constant,
                                                                                   InEditMode = VDefinition.EditMode.Constant,
                                                                                   OutEditMode = VDefinition.EditMode.Constant,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 0), newCurveX);

                var newCurveY = new Curve();
                newCurveY.AddOrUpdateV(EvaluationContext.GlobalTimeInBars, new VDefinition()
                                                                               {
                                                                                   Value = size2InputSlot.Value.Height,
                                                                                   InType = VDefinition.Interpolation.Constant,
                                                                                   OutType = VDefinition.Interpolation.Constant,
                                                                                   InEditMode = VDefinition.EditMode.Constant,
                                                                                   OutEditMode = VDefinition.EditMode.Constant,
                                                                               });
                _animatedInputCurves.Add(new CurveId(inputSlot, 1), newCurveY);

                size2InputSlot.UpdateAction = context =>
                                                {
                                                    size2InputSlot.Value.Width = (int)newCurveX.GetSampledValue(context.TimeInBars);
                                                    size2InputSlot.Value.Height = (int)newCurveY.GetSampledValue(context.TimeInBars);
                                                };
                size2InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }            
            else
            {
                Log.Error("Could not create update action.");
            }
        }

        internal void CreateUpdateActionsForExistingCurves(IEnumerable<Instance> childInstances)
        {
            // gather all inputs that correspond to stored ids
            var relevantInputs = from curveEntry in _animatedInputCurves
                                 from childInstance in childInstances
                                 where curveEntry.Key.InstanceId == childInstance.SymbolChildId
                                 from inputSlot in childInstance.Inputs
                                 where curveEntry.Key.InputId == inputSlot.Id
                                 group (inputSlot, curveEntry.Value) by (Id: childInstance.SymbolChildId, inputSlot.Id)
                                 into inputGroup
                                 select inputGroup;

            foreach (var groupEntry in relevantInputs)
            {
                var count = groupEntry.Count();
                if (count == 1)
                {
                    var (inputSlot, curve) = groupEntry.First();
                    if (inputSlot is Slot<float> typedInputSlot)
                    {
                        typedInputSlot.UpdateAction = context => { typedInputSlot.Value = (float)curve.GetSampledValue(context.TimeInBars); };
                        typedInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                    else if (inputSlot is Slot<int> intSlot)
                    {
                        intSlot.UpdateAction = context => { intSlot.Value = (int)curve.GetSampledValue(context.TimeInBars); };
                        intSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else if (count == 2)
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].inputSlot;
                    if (inputSlot is Slot<Vector2> vector2InputSlot)
                    {
                        vector2InputSlot.UpdateAction = context =>
                                                        {
                                                            vector2InputSlot.Value.X = (float)entries[0].Value.GetSampledValue(context.TimeInBars);
                                                            vector2InputSlot.Value.Y = (float)entries[1].Value.GetSampledValue(context.TimeInBars);
                                                        };
                        vector2InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else if (count == 3)
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].inputSlot;
                    if (inputSlot is Slot<Vector3> vector3InputSlot)
                    {
                        vector3InputSlot.UpdateAction = context =>
                                                        {
                                                            vector3InputSlot.Value.X = (float)entries[0].Value.GetSampledValue(context.TimeInBars);
                                                            vector3InputSlot.Value.Y = (float)entries[1].Value.GetSampledValue(context.TimeInBars);
                                                            vector3InputSlot.Value.Z = (float)entries[2].Value.GetSampledValue(context.TimeInBars);
                                                        };
                        vector3InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else if (count == 4)
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].inputSlot;
                    if (inputSlot is Slot<Vector4> vector4InputSlot)
                    {
                        vector4InputSlot.UpdateAction = context =>
                                                        {
                                                            vector4InputSlot.Value.X = (float)entries[0].Value.GetSampledValue(context.TimeInBars);
                                                            vector4InputSlot.Value.Y = (float)entries[1].Value.GetSampledValue(context.TimeInBars);
                                                            vector4InputSlot.Value.Z = (float)entries[2].Value.GetSampledValue(context.TimeInBars);
                                                            vector4InputSlot.Value.W = (float)entries[3].Value.GetSampledValue(context.TimeInBars);
                                                        };
                        vector4InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }
                }
                else
                {
                    Debug.Assert(false);
                }
            }
        }

        public void RemoveAnimationFrom(IInputSlot inputSlot)
        {
            inputSlot.SetUpdateActionBackToDefault();
            inputSlot.DirtyFlag.Trigger &= ~DirtyFlagTrigger.Animated;
            var curveKeysToRemove = (from curveId in _animatedInputCurves.Keys
                                     where curveId.InstanceId == inputSlot.Parent.SymbolChildId
                                     where curveId.InputId == inputSlot.Id
                                     select curveId).ToArray(); // ToArray is needed to remove from collection in batch
            foreach (var curveKey in curveKeysToRemove)
            {
                _animatedInputCurves.Remove(curveKey);
            }
        }

        public bool IsInputSlotAnimated(IInputSlot inputSlot)
        {
            return _animatedInputCurves.ContainsKey(new CurveId(inputSlot));
        }

        public bool IsAnimated(Guid symbolChildId, Guid inputId) 
        {
            return _animatedInputCurves.ContainsKey(new CurveId(symbolChildId, inputId));
        }
 
        public bool IsInstanceAnimated(Instance instance)
        {
            using (var e = _animatedInputCurves.Keys.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (e.Current.InstanceId == instance.SymbolChildId)
                    {
                        return true;
                    }
                }

                return false;
            }

            // code above generates way less allocations than the line below:
            // return _animatedInputCurves.Any(c => c.Key.InstanceId == instance.Id);
        }

        public IEnumerable<Curve> GetCurvesForInput(IInputSlot inputSlot)
        {
            return from curve in _animatedInputCurves
                   where curve.Key.InstanceId == inputSlot.Parent.SymbolChildId
                   where curve.Key.InputId == inputSlot.Id
                   orderby curve.Key.Index
                   select curve.Value;
        }

        public void Write(JsonTextWriter writer)
        {
            if (_animatedInputCurves.Count == 0)
                return;

            writer.WritePropertyName("Animator");
            writer.WriteStartArray();

            foreach (var entry in _animatedInputCurves)
            {
                writer.WriteStartObject();

                writer.WriteValue("InstanceId", entry.Key.InstanceId);
                writer.WriteValue("InputId", entry.Key.InputId);
                if (entry.Key.Index != 0)
                {
                    writer.WriteValue("Index", entry.Key.Index);
                }

                entry.Value.Write(writer); // write curve itself

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public void Read(JToken inputToken)
        {
            foreach (JToken entry in inputToken)
            {
                Guid instanceId = Guid.Parse(entry["InstanceId"].Value<string>());
                Guid inputId = Guid.Parse(entry["InputId"].Value<string>());
                var indexToken = entry.SelectToken("Index");
                int index = indexToken?.Value<int>() ?? 0;
                Curve curve = new Curve();
                curve.Read(entry);

                _animatedInputCurves.Add(new CurveId(instanceId, inputId, index), curve);
            }
        }

        private readonly Dictionary<CurveId, Curve> _animatedInputCurves = new Dictionary<CurveId, Curve>();

        public static void UpdateVector3InputValue(InputSlot<Vector3> inputSlot, Vector3 value)
        {
            var animator = inputSlot.Parent.Parent.Symbol.Animator;
            if (animator.IsInputSlotAnimated(inputSlot))
            {
                var curves = animator.GetCurvesForInput(inputSlot).ToArray();
                double time = EvaluationContext.GlobalTimeInBars;
                SharpDX.Vector3 newValue = new SharpDX.Vector3(value.X, value.Y, value.Z);
                for (int i = 0; i < 3; i++)
                {
                    var key = curves[i].GetV(time);
                    if (key == null)
                        key = new VDefinition() { U = time };
                    key.Value = newValue[i];
                    curves[i].AddOrUpdateV(time, key);
                }
            }
            else
            {
                inputSlot.SetTypedInputValue(value);
            }
        }

        public static void UpdateFloatInputValue(InputSlot<float> inputSlot, float value)
        {
            var animator = inputSlot.Parent.Parent.Symbol.Animator;
            if (animator.IsInputSlotAnimated(inputSlot))
            {
                var curve = animator.GetCurvesForInput(inputSlot).First();
                double time = EvaluationContext.GlobalTimeInBars;
                var key = curve.GetV(time);
                if (key == null)
                    key = new VDefinition() { U = time };
                key.Value = value;
                curve.AddOrUpdateV(time, key);
            }
            else
            {
                inputSlot.SetTypedInputValue(value);
            }
        }
    }
}