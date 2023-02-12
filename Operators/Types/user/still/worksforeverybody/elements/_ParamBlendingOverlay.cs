using T3.Core.DataTypes;
using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9df0e38e_ccf1_405a_ab18_6586e652cdf1
{
    public class _ParamBlendingOverlay : Instance<_ParamBlendingOverlay>
    {
        [Output(Guid = "9158ca50-6368-4266-9985-0f60b3e2b560")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "d19f4570-225e-4de5-b86e-3499011dd4e1")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();

        [Output(Guid = "2c1800cd-ea63-4cd5-baf4-582a8127d50f")]
        public readonly Slot<float> Result = new Slot<float>();


    }
}

