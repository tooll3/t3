using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator
{
    public class VariationSelector
    {
        public int Index1 { get; set; } = 0;
        public int Index2 { get; set; } = 0;
        public float Weight { get; set; } = 0.5f;
    }

    public class Variator : SymbolExtension
    {
        public struct VariationId
        {
            public VariationId(Guid instanceId, Guid inputId, int index = 0)
            {
                InstanceId = instanceId;
                InputId = inputId;
            }

            public VariationId(IInputSlot inputSlot, int index = 0)
            {
                InstanceId = inputSlot.Parent.SymbolChildId;
                InputId = inputSlot.Id;
            }

            public readonly Guid InstanceId;
            public readonly Guid InputId;
            public static VariationId EmptySet = new VariationId(Guid.Empty, Guid.Empty);
        }

        public void AddVariationTo(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> floatInputSlot)
            {
                var newVariation = new Variation(floatInputSlot.Value);
                newVariation.Values.Add(floatInputSlot.Value + 5.0f); // for testing
                _inputVariations.Add(new VariationId(inputSlot), newVariation);

                floatInputSlot.UpdateAction = context =>
                                              {
                                                  context.VariationOverwrites.TryGetValue(VariationId.EmptySet, out var selector);
                                                  floatInputSlot.Value = newVariation.GetVariedValue(selector);
                                              };
                floatInputSlot.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
            }
        }

        public void RemoveVariationFrom(IInputSlot inputSlot)
        {
            inputSlot.SetUpdateActionBackToDefault();
            inputSlot.DirtyFlag.Trigger &= ~DirtyFlagTrigger.Animated;
            inputSlot.DirtyFlag.Invalidate(true);
            _inputVariations.Remove(new VariationId(inputSlot));
        }

        public bool IsInputSlotVaried(IInputSlot inputSlot) => _inputVariations.ContainsKey(new VariationId(inputSlot));

        public Variation GetVariationForInput(IInputSlot inputSlot)
        {
            return _inputVariations.TryGetValue(new VariationId(inputSlot), out var variation) ? variation : null;
        }

        public class Variation
        {
            public Variation(float initialValue)
            {
                Values.Add(initialValue);
            }

            public List<float> Values { get; } = new List<float>(10);

            public float GetVariedValue(VariationSelector selector)
            {
                if (selector == null)
                    selector = Selector;

                return GetBlendedValue(selector.Index1, selector.Index2, selector.Weight);
            }

            public float GetBlendedValue(int index1, int index2, float weight)
            {
                int max = Values.Count - 1;
                index1 = index1.Clamp(0, max);
                index2 = index2.Clamp(0, max);

                return MathUtils.Lerp(Values[index1], Values[index2], weight);
            }

            public VariationSelector Selector { get; } = new VariationSelector();
        }

        private readonly Dictionary<VariationId, Variation> _inputVariations = new Dictionary<VariationId, Variation>(20);
    }
}