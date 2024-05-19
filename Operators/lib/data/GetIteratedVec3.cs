using System.Numerics;
using System.Reflection;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_d86e9585_d233_455a_9059_fa93debfed01
{
    public class GetIteratedVec3 : Instance<GetIteratedVec3>
    {
        [Output(Guid = "E9839288-05DB-41E7-8326-3BE4F65F2410", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector3> Result = new();

        public GetIteratedVec3()
        {
            Result.UpdateAction = Update;
            Result.DirtyFlag.Trigger |= DirtyFlagTrigger.Animated;
        }

        private void Update(EvaluationContext context)
        {
            Result.Value = StructuredListUtils.GetIteratedValueOfFieldWithType<Vector3>(context, FieldName, ref _field);
        }

        private FieldInfo _field;


        [Input(Guid = "18dc4a34-f0c1-4763-be36-71b59533916a")]
        public readonly InputSlot<string> FieldName = new();
    }
}