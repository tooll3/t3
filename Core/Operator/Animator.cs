#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator.Slots;
using T3.Serialization;
using T3.Core.Utils;
using T3.Core.Utils.Geometry;
using Int3 = T3.Core.DataTypes.Vector.Int3;

namespace T3.Core.Operator;

public class SymbolExtension
{
    // todo: how is a symbol extension defined, what exactly does this mean
}

public sealed class Animator : SymbolExtension
{
    private record struct CurveId
    {
        public CurveId(Guid symbolChildId, Guid inputId, int index = 0)
        {
            SymbolChildId = symbolChildId;
            InputId = inputId;
            Index = index;
        }

        public CurveId(IInputSlot inputSlot, int index = 0)
        {
            SymbolChildId = inputSlot.Parent.SymbolChildId;
            InputId = inputSlot.Id;
            Index = index;
        }

        public Guid SymbolChildId;
        public Guid InputId;
        public int Index;
    }

    public void CopyAnimationsTo(Animator targetAnimator, List<Guid> childrenToCopyAnimationsFrom, Dictionary<Guid, Guid> oldToNewIdDict)
    {
        foreach (var (id, curve) in _animatedInputCurves)
        {
            if (!childrenToCopyAnimationsFrom.Contains(id.SymbolChildId))
                continue;

            CloneAndAddCurve(targetAnimator, oldToNewIdDict, id, curve);
        }
    }

    public void RemoveAnimationsFromInstances(List<Guid> instanceIds)
    {
        List<CurveId> elementsToDelete = [];
        foreach (var (id, _) in _animatedInputCurves)
        {
            if (!instanceIds.Contains(id.SymbolChildId))
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
        Guid newInstanceId = oldToNewIdDict[id.SymbolChildId];
        var newCurveId = new CurveId(newInstanceId, id.InputId, id.Index);
        var newCurve = curve.TypedClone();
        targetAnimator._animatedInputCurves.Add(newCurveId, newCurve);
    }

    public Curve[]? AddOrRestoreCurvesToInput(IInputSlot inputSlot, Curve[]? originalCurves)
    {
        switch (inputSlot)
        {
            case Slot<float> floatInputSlot:
                return AddCurvesForFloatValue(inputSlot, [floatInputSlot.Value], originalCurves);
            case Slot<Vector2> vector2InputSlot:
                return AddCurvesForFloatValue(inputSlot, vector2InputSlot.Value.ToArray(), originalCurves);
            case Slot<Vector3> vector3InputSlot:
                return AddCurvesForFloatValue(inputSlot, vector3InputSlot.Value.ToArray(), originalCurves);
            case Slot<Vector4> vector4InputSlot:
                return AddCurvesForFloatValue(inputSlot, vector4InputSlot.Value.ToArray(), originalCurves);
            case Slot<int> intInputSlot:
                return AddCurvesForIntValue(inputSlot, [intInputSlot.Value], originalCurves);
            case Slot<Int2> size2InputSlot:
                return AddCurvesForIntValue(inputSlot, [size2InputSlot.Value.Width, size2InputSlot.Value.Height], originalCurves);
            case Slot<bool> boolInputSlot:
                return AddCurvesForIntValue(inputSlot, [boolInputSlot.Value ? 1 : 0], originalCurves);
            default:
                Log.Error("Could not create curves for this type");
                break;
        }

        return null;
    }

    private Curve[] AddCurvesForFloatValue(IInputSlot inputSlot, float[] values, Curve[]? originalCurves)
    {
        var curves = originalCurves ?? new Curve[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if (originalCurves == null)
                curves[index] = new Curve();

            curves[index].AddOrUpdateV(Playback.Current.TimeInBars, new VDefinition()
                                                                        {
                                                                            Value = values[index],
                                                                            InType = VDefinition.Interpolation.Spline,
                                                                            OutType = VDefinition.Interpolation.Spline,
                                                                        });
            _animatedInputCurves.Add(new CurveId(inputSlot, index), curves[index]);
        }

        return curves;
    }

    private Curve[] AddCurvesForIntValue(IInputSlot inputSlot, int[] values, Curve[]? originalCurves)
    {
        var curves = originalCurves ?? new Curve[values.Length];
        for (var index = 0; index < values.Length; index++)
        {
            if (originalCurves == null)
                curves[index] = new Curve();

            curves[index].AddOrUpdateV(Playback.Current.TimeInBars, new VDefinition()
                                                                        {
                                                                            Value = values[index],
                                                                            InType = VDefinition.Interpolation.Constant,
                                                                            OutType = VDefinition.Interpolation.Constant,
                                                                            InEditMode = VDefinition.EditMode.Constant,
                                                                            OutEditMode = VDefinition.EditMode.Constant,
                                                                        });
            _animatedInputCurves.Add(new CurveId(inputSlot, index), curves[index]);
        }

        return curves;
    }

    private readonly record struct InstanceCurve(CurveId Id, Curve Value, Instance CorrespondingInstance);

    private readonly struct InstanceCurveInput(in InstanceCurve instanceCurve, IInputSlot inputSlot)
    {
        public readonly CurveId Id = instanceCurve.Id;
        public readonly Curve Curve = instanceCurve.Value;
        public readonly Instance CorrespondingInstance = instanceCurve.CorrespondingInstance;
        public readonly IInputSlot InputSlot = inputSlot;
    }

    private readonly record struct InputSlotCurve(IInputSlot InputSlot, Curve Curve);

    private readonly record struct OrderedAnimationCurve(in CurveId Id, Curve Curve);

    // ReSharper disable NotAccessedPositionalProperty.Local
    private readonly record struct InputGroupKey(in Guid SymbolChildId, in Guid InputId, Instance ChildInstance);
    // ReSharper restore NotAccessedPositionalProperty.Local

    internal void CreateUpdateActionsForExistingCurves(IEnumerable<Instance> childInstances)
    {
        // gather all inputs that correspond to stored ids
        var relevantInputs = _animatedInputCurves
                             // first sort them 
                            .OrderBy(valuePair => valuePair.Key.SymbolChildId)
                            .ThenBy(valuePair => valuePair.Key.InputId)
                            .ThenBy(valuePair => valuePair.Key.Index)
                            .Select(kvp => new OrderedAnimationCurve(kvp.Key, kvp.Value))

                             // then match instances to the curves
                            .SelectMany(_ => childInstances, (curve, childInstance) => new InstanceCurve(curve.Id, curve.Curve, childInstance))
                            .Where(instanceCurve => instanceCurve.Id.SymbolChildId == instanceCurve.CorrespondingInstance.SymbolChildId)

                             // match each matching instance's input with its corresponding curve
                            .SelectMany(instanceCurve => instanceCurve.CorrespondingInstance.Inputs,
                                        (instanceCurve, inputSlot) => new InstanceCurveInput(instanceCurve, inputSlot))
                            .Where(instanceCurveInput => instanceCurveInput.Id.InputId == instanceCurveInput.InputSlot.Id)

                             // finally group them by input of specific child instances
                            .GroupBy(
                                     instanceCurveInput => new InputGroupKey(instanceCurveInput.CorrespondingInstance.SymbolChildId,
                                                                             instanceCurveInput.InputSlot.Id,
                                                                             instanceCurveInput.CorrespondingInstance),
                                     instanceCurveInput => new InputSlotCurve(instanceCurveInput.InputSlot, instanceCurveInput.Curve)
                                    )
                            .ToArray();

        foreach (var groupEntry in relevantInputs)
        {
            switch (groupEntry.Count())
            {
                case 1:
                {
                    var (inputSlot, curve) = groupEntry.First();
                    switch (inputSlot)
                    {
                        case Slot<float> typedInputSlot:
                            typedInputSlot.OverrideWithAnimationAction(context => { typedInputSlot.Value = (float)curve.GetSampledValue(context.LocalTime); });
                            typedInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                            break;
                        case Slot<int> intSlot:
                            intSlot.OverrideWithAnimationAction(context => { intSlot.Value = (int)curve.GetSampledValue(context.LocalTime); });
                            intSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                            break;
                        case Slot<bool> boolSlot:
                            boolSlot.OverrideWithAnimationAction(context => { boolSlot.Value = curve.GetSampledValue(context.LocalTime) > 0.5f; });
                            boolSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                            break;
                    }

                    break;
                }
                case 2:
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].InputSlot;
                    switch (inputSlot)
                    {
                        case Slot<System.Numerics.Vector2> vector2InputSlot:
                            vector2InputSlot.OverrideWithAnimationAction(context =>
                                                                         {
                                                                             vector2InputSlot.Value.X =
                                                                                 (float)entries[0].Curve.GetSampledValue(context.LocalTime);
                                                                             vector2InputSlot.Value.Y =
                                                                                 (float)entries[1].Curve.GetSampledValue(context.LocalTime);
                                                                         });
                            vector2InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                            break;
                        case Slot<Int2> size2InputSlot:
                            size2InputSlot.OverrideWithAnimationAction(context =>
                                                                       {
                                                                           size2InputSlot.Value.Width =
                                                                               (int)entries[0].Curve.GetSampledValue(context.LocalTime);
                                                                           size2InputSlot.Value.Height =
                                                                               (int)entries[1].Curve.GetSampledValue(context.LocalTime);
                                                                       });
                            size2InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                            break;
                    }

                    break;
                }
                case 3:
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].InputSlot;
                    switch (inputSlot)
                    {
                        case Slot<System.Numerics.Vector3> vector3InputSlot:
                            vector3InputSlot.OverrideWithAnimationAction(context =>
                                                                         {
                                                                             vector3InputSlot.Value.X =
                                                                                 (float)entries[0].Curve.GetSampledValue(context.LocalTime);
                                                                             vector3InputSlot.Value.Y =
                                                                                 (float)entries[1].Curve.GetSampledValue(context.LocalTime);
                                                                             vector3InputSlot.Value.Z =
                                                                                 (float)entries[2].Curve.GetSampledValue(context.LocalTime);
                                                                         });
                            vector3InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                            break;
                        case Slot<Int3> int3InputSlot:
                            int3InputSlot.OverrideWithAnimationAction(context =>
                                                                      {
                                                                          int3InputSlot.Value.X = (int)entries[0].Curve.GetSampledValue(context.LocalTime);
                                                                          int3InputSlot.Value.Y = (int)entries[1].Curve.GetSampledValue(context.LocalTime);
                                                                          int3InputSlot.Value.Z = (int)entries[2].Curve.GetSampledValue(context.LocalTime);
                                                                      });
                            int3InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                            break;
                    }

                    break;
                }
                case 4:
                {
                    var entries = groupEntry.ToArray();
                    var inputSlot = entries[0].InputSlot;
                    if (inputSlot is Slot<System.Numerics.Vector4> vector4InputSlot)
                    {
                        vector4InputSlot.OverrideWithAnimationAction(context =>
                                                                     {
                                                                         vector4InputSlot.Value.X = (float)entries[0].Curve.GetSampledValue(context.LocalTime);
                                                                         vector4InputSlot.Value.Y = (float)entries[1].Curve.GetSampledValue(context.LocalTime);
                                                                         vector4InputSlot.Value.Z = (float)entries[2].Curve.GetSampledValue(context.LocalTime);
                                                                         vector4InputSlot.Value.W = (float)entries[3].Curve.GetSampledValue(context.LocalTime);
                                                                     });
                        vector4InputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
                    }

                    break;
                }
                default:
                    Debug.Assert(false);
                    break;
            }
        }
    }

    private IOrderedEnumerable<KeyValuePair<CurveId, Curve>> OrderedAnimationCurves
    {
        get
        {
            var orderedCurves = _animatedInputCurves
                               .OrderBy(valuePair => valuePair.Key.SymbolChildId)
                               .ThenBy(valuePair => valuePair.Key.InputId)
                               .ThenBy(valuePair => valuePair.Key.Index);
            return orderedCurves;
        }
    }

    public void RemoveAnimationFrom(IInputSlot inputSlot)
    {
        inputSlot.RestoreUpdateAction();
        inputSlot.DirtyFlag.Trigger &= ~DirtyFlagTrigger.Animated;
        var curveKeysToRemove = (from curveId in _animatedInputCurves.Keys
                                 where curveId.SymbolChildId == inputSlot.Parent.SymbolChildId
                                 where curveId.InputId == inputSlot.Id
                                 select curveId).ToArray(); // ToArray is needed to remove from collection in batch
        foreach (var curveKey in curveKeysToRemove)
        {
            _animatedInputCurves.Remove(curveKey);
        }
    }

    public bool TryGetFirstInputAnimationCurve(IInputSlot inputSlot, [NotNullWhen(true)] out Curve? curve)
    {
        return _animatedInputCurves.TryGetValue(new CurveId(inputSlot), out curve);
    }

    private static CurveId _lookUpKey;

    public bool IsInputSlotAnimated(IInputSlot inputSlot)
    {
        _lookUpKey.SymbolChildId = inputSlot.Parent.SymbolChildId;
        _lookUpKey.InputId = inputSlot.Id;
        _lookUpKey.Index = 0;
        return _animatedInputCurves.ContainsKey(_lookUpKey);
    }

    public bool IsInputAnimated(Symbol.Child symbolChild, Symbol.Child.Input input)
    {
        _lookUpKey.SymbolChildId = symbolChild.Id;
        _lookUpKey.InputId = input.Id;
        _lookUpKey.Index = 0;
        return _animatedInputCurves.ContainsKey(_lookUpKey);
    }
    
    public bool IsAnimated(Guid symbolChildId, Guid inputId)
    {
        _lookUpKey.SymbolChildId = symbolChildId;
        _lookUpKey.InputId = inputId;
        _lookUpKey.Index = 0;

        return _animatedInputCurves.ContainsKey(_lookUpKey);
    }

    public bool IsInstanceAnimated(Instance instance)
    {
        using var e = _animatedInputCurves.Keys.GetEnumerator();
        while (e.MoveNext())
        {
            if (e.Current.SymbolChildId == instance.SymbolChildId)
            {
                return true;
            }
        }

        return false;

        // code above generates way less allocations than the line below:
        // return _animatedInputCurves.Any(c => c.Key.InstanceId == instance.Id);
    }

    public IEnumerable<Curve> GetCurvesForInput(Guid symbolChildId, Guid inputId)
    {
        return from curve in _animatedInputCurves
               where curve.Key.SymbolChildId == symbolChildId
               where curve.Key.InputId == inputId
               orderby curve.Key.Index
               select curve.Value;
    }
    
    public IEnumerable<Curve> GetCurvesForInput(IInputSlot inputSlot)
    {
        return from curve in _animatedInputCurves
               where curve.Key.SymbolChildId == inputSlot.Parent.SymbolChildId
               where curve.Key.InputId == inputSlot.Id
               orderby curve.Key.Index
               select curve.Value;
    }

    public IEnumerable<VDefinition> GetTimeKeys(Guid symbolChildId, Guid inputId, double time)
    {
        var curves = from curve in _animatedInputCurves
                     where curve.Key.SymbolChildId == symbolChildId
                     where curve.Key.InputId == inputId
                     orderby curve.Key.Index
                     select curve.Value;

        foreach (var curve in curves)
        {
            yield return curve.GetV(time);
        }
    }

    public void SetTimeKeys(Guid symbolChildId, Guid inputId, double time, List<VDefinition> vDefinitions)
    {
        var curves = from curve in _animatedInputCurves
                     where curve.Key.SymbolChildId == symbolChildId
                     where curve.Key.InputId == inputId
                     orderby curve.Key.Index
                     select curve.Value;

        var index = 0;
        foreach (var curve in curves)
        {
            var vDef = vDefinitions[index++];
            if (vDef == null)
            {
                curve.RemoveKeyframeAt(time);
            }
            else
            {
                curve.AddOrUpdateV(time, vDef);
            }
        }
    }

    internal void Write(JsonTextWriter writer)
    {
        if (_animatedInputCurves.Count == 0)
            return;

        writer.WritePropertyName("Animator");
        writer.WriteStartArray();

        foreach (var (key, curve) in _animatedInputCurves.ToList().OrderBy(valuePair => valuePair.Key.Index))
        {
            writer.WriteStartObject();

            writer.WriteValue("InstanceId", key.SymbolChildId); // TODO: "InstanceId" is a misleading identifier
            writer.WriteValue("InputId", key.InputId);
            if (key.Index != 0)
            {
                writer.WriteValue("Index", key.Index);
            }

            curve.Write(writer); // write curve itself

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    internal void Read(JToken inputToken, Symbol symbol)
    {
        var curves = new List<KeyValuePair<CurveId, Curve>>();
        foreach (JToken entry in inputToken)
        {
            JsonUtils.TryGetGuid(entry["InstanceId"], out var symbolChildId);
            JsonUtils.TryGetGuid(entry["InputId"], out var inputId);
            var indexToken = entry.SelectToken("Index");
            var index = indexToken?.Value<int>() ?? 0;
            var curve = new Curve();

            if (!symbol.Children.ContainsKey(symbolChildId))
                continue;

            curve.Read(entry);
            curves.Add(new KeyValuePair<CurveId, Curve>(new CurveId(symbolChildId, inputId, index), curve));
        }

        foreach (var (key, value) in curves.OrderBy(curveId => curveId.Key.Index))
        {
            _animatedInputCurves.Add(key, value);
        }
    }

    /// <summary>
    /// This is used when loading operators 
    /// </summary>
    public void AddCurvesToInput(List<Curve> curves, IInputSlot inputSlot)
    {
        if (inputSlot.Parent.Parent == null)
        {
            Log.Warning("Can't add animations curves without valid structure.");
            return;
        }

        for (var index = 0; index < curves.Count; index++)
        {
            var curve = curves[index];
            var curveId = new CurveId(inputSlot.Parent.SymbolChildId, inputSlot.Id, index);
            _animatedInputCurves.Add(curveId, curve);
        }

        inputSlot.Parent.Parent.Symbol.CreateOrUpdateActionsForAnimatedChildren();
    }

    private readonly Dictionary<CurveId, Curve> _animatedInputCurves = new();

    public static void UpdateVector3InputValue(InputSlot<System.Numerics.Vector3> inputSlot, Vector3 value)
    {
        if (inputSlot.Parent.Parent == null)
        {
            Log.Warning("Can't add animations curves without valid structure.");
            return;
        }

        var animator = inputSlot.Parent.Parent.Symbol.Animator;
        if (animator.IsInputSlotAnimated(inputSlot))
        {
            var curves = animator.GetCurvesForInput(inputSlot).ToArray();
            double time = Playback.Current.TimeInBars;
            Vector3 newValue = new Vector3(value.X, value.Y, value.Z);
            for (int i = 0; i < 3; i++)
            {
                var key = curves[i].GetV(time);
                if (key == null)
                    key = new VDefinition() { U = time };
                key.Value = newValue.GetValueUnsafe(i);
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
        if (inputSlot.Parent.Parent == null)
        {
            Log.Warning("Can't add animations curves without valid structure.");
            return;
        }

        var animator = inputSlot.Parent.Parent.Symbol.Animator;
        if (animator.IsInputSlotAnimated(inputSlot))
        {
            var curve = animator.GetCurvesForInput(inputSlot).First();
            double time = Playback.Current.TimeInBars;
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