using System;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_bc11f5b6_ee3b_4c17_9faa_8af98835206e
{
    public class _SlotMachineRoll : Instance<_SlotMachineRoll>
    {
        [Output(Guid = "36676a26-a315-44bf-94a9-527487440072")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "3da5fc85-c264-44de-a6bf-5f333acd89db")]
        public readonly InputSlot<string> InputString = new InputSlot<string>();

        [Input(Guid = "a1130283-0071-4e3d-bcb0-f637cc7988e9")]
        public readonly InputSlot<float> FocusIndex = new InputSlot<float>();

        [Input(Guid = "9de3586e-fdaa-40a9-b0f1-1d6e8b4b54a0")]
        public readonly InputSlot<bool> TriggerRoll = new InputSlot<bool>();

        [Input(Guid = "05f3d981-8dd6-4f6d-9a6c-f2da3fe3f8de")]
        public readonly InputSlot<int> Seed = new InputSlot<int>();

    }
}

