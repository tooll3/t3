using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_780edb20_f83f_494c_ab17_7015e2311250
{
    public class RepeatAtGPoints : Instance<RepeatAtGPoints>
    {

        [Output(Guid = "3ac76b2a-7b1c-4762-a3f6-50529cd42fa8")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "47c3c549-78bb-41fd-a88c-58f643870b40")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "a952d91a-a86b-4370-acd9-e17b19025966")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GTargets = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "f15a003c-7969-4505-b598-6c6c4b5a3bbe")]
        public readonly InputSlot<bool> ApplyTargetOrientation = new InputSlot<bool>();

        [Input(Guid = "f71ddebe-1f2c-47d0-ba39-eb5c4693e909")]
        public readonly InputSlot<bool> ApplyTargetScaleW = new InputSlot<bool>();

        [Input(Guid = "f582aa39-f5e0-46ad-89ae-6f29ab60d3e6")]
        public readonly InputSlot<bool> MultiplyTargetW = new InputSlot<bool>();
    }
}

