using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_eeab67a7_5e52_4b85_a993_a728bed78278
{
    public class VariationOverwrite : Instance<VariationOverwrite>
    {
        [Output(Guid = "c02198ca-0839-45e7-894d-f3d7a1b05fe4")]
        public readonly Slot<float> Result = new Slot<float>();

        public VariationOverwrite()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var overwrites = context.VariationOverwrites;
            // var id = new Variator.VariationId(Guid.Empty, Guid.Empty);
            if (!overwrites.TryGetValue(Variator.VariationId.EmptySet, out var entry))
            {
                entry = new VariationSelector();
                overwrites.Add(Variator.VariationId.EmptySet, entry);
            }

            entry.Index1 = Index1.GetValue(context);
            entry.Index2 = Index2.GetValue(context);
            entry.Weight = Weight.GetValue(context);
            Result.Value = Input.GetValue(context);
            overwrites.Remove(Variator.VariationId.EmptySet);
        }
        
        [Input(Guid = "0ec56e2d-8cdc-4378-97fb-9f0020312c90")]
        public readonly InputSlot<float> Input = new InputSlot<float>();

        [Input(Guid = "0aff71f6-e5c2-4970-9e60-f9f919a61912")]
        public readonly InputSlot<int> Index1 = new InputSlot<int>();

        [Input(Guid = "62cacb5f-3511-45df-9b24-66ea1f2d7f60")]
        public readonly InputSlot<int> Index2 = new InputSlot<int>();

        [Input(Guid = "14295e06-75d5-483e-95b5-527936fd5e94")]
        public readonly InputSlot<float> Weight = new InputSlot<float>();
    }
}