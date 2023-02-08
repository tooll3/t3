using System;
using System;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3f7f8975_a7bb_47e6_96f7_061deb418e18
{
    public class _WFEGameUi : Instance<_WFEGameUi>
    {
        [Output(Guid = "9f6bd5f3-eb31-40de-894a-51d55b4e0fa3")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "8d294996-7f87-424e-9869-12d383cb7869")]
        public readonly InputSlot<bool> IsActive = new InputSlot<bool>();

        [Input(Guid = "dd0ca7a1-9fa1-4364-80a4-ecf0892fa66b")]
        public readonly InputSlot<int> SceneIndex = new InputSlot<int>();

        [Input(Guid = "cee4bca4-e9d4-493d-b4dd-56dd69d7fb3f")]
        public readonly InputSlot<float> PerfectTime = new InputSlot<float>();

    }
}

