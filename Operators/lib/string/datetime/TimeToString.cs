using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_075612b1_8760_4858_ad6b_6c85a7716794
{
    public class TimeToString : Instance<TimeToString>
    {
        [Output(Guid = "d45912c1-1c26-4a80-bfff-57de6ae6ccdf")]
        public readonly Slot<string> Result = new();

        [Input(Guid = "ecc27b89-e89a-4c01-93ad-b4750d09f2f7")]
        public readonly InputSlot<float> Input = new();


    }
}

