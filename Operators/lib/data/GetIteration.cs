using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4c088b67_be47_4599_bd64_5f277abb0113
{
    public class GetIteration : Instance<GetIteration>
    {
        [Output(Guid = "c3a199dc-993c-4f43-a4aa-f355e6584a64", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Progress = new();

        [Output(Guid = "821FD3F7-2956-44E0-9412-6D33D1EDB016", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> Index = new();

        public GetIteration()
        {
            Progress.UpdateAction = Update;
            Index.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (context.IteratedList == null || context.IteratedList.NumElements == 0)
                return;


            Index.Value = context.IteratedListIndex;
            Progress.Value = (float)context.IteratedListIndex/(context.IteratedList.NumElements - 1);
        }
    }
}