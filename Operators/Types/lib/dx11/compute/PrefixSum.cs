using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d35a403b_7e6e_4725_a344_6e8008a4e1e1
{
    public class PrefixSum : Instance<PrefixSum>
    {
        [Output(Guid = "a0801b0a-3447-4179-aa12-8b4b088868d2")]
        public readonly Slot<Command> Output = new();

        [Output(Guid = "faeb2a7e-de0f-4497-964b-7b21dd56f525")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> ResultBuffer = new();

        [Input(Guid = "c5561f3b-495e-47e1-95d4-ea3a750f1842")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> InputList2 = new();

    }
}

