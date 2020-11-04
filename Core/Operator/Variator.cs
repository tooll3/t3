using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Operator.Slots;

namespace T3.Core.Operator
{
    public class VariationSetup
    {
        public int Index1 { get; set; } = 0;
        public int Index2 { get; set; } = 0;
        public float Weight { get; set; } = 0.5f;
    }

    public class Variator : SymbolExtension
    {
        struct VariationId
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
        }

        public void AddVariationTo(IInputSlot inputSlot)
        {
            if (inputSlot is Slot<float> floatInputSlot)
            {
                var newVariation = new Variation(floatInputSlot.Value);
                _inputVariations.Add(new VariationId(inputSlot), newVariation);

                floatInputSlot.UpdateAction = context => { floatInputSlot.Value = newVariation.GetVariedValue(context.VariationSetup); };
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
            if (_inputVariations.TryGetValue(new VariationId(inputSlot), out var variation))
                return variation;

            return null;
        }

        public class Variation
        {
            public Variation(float initialValue)
            {
                Values.Add(initialValue);
            }

            public List<float> Values { get; } = new List<float>(10);

            public float GetVariedValue(VariationSetup setup)
            {
                return GetBlendedValue(setup.Index1, setup.Index2, setup.Weight);
            }

            public float GetBlendedValue(int index1, int index2, float weight)
            {
                int max = Values.Count - 1;
                index1 = index1.Clamp(0, max);
                index2 = index2.Clamp(0, max);

                return MathUtils.Lerp(Values[index1], Values[index2], weight);
            }
        }

        private readonly Dictionary<VariationId, Variation> _inputVariations = new Dictionary<VariationId, Variation>(20);
    }
}