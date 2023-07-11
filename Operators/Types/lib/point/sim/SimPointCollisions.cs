using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ed0bc47a_31ef_400b_b4e4_5552a859b309
{
    public class SimPointCollisions : Instance<SimPointCollisions>
    {

        [Output(Guid = "28ebbcd9-737d-4c94-8c3b-e67318b06374")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "7ec04bd6-80c3-4537-b333-f42f0c016730")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> PointsA_ = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "cb2b3cb9-798c-4875-acd7-dd830e110201")]
        public readonly InputSlot<float> Threshold = new InputSlot<float>();

        [Input(Guid = "64996b47-cf36-46c4-96cc-424547714bd7")]
        public readonly InputSlot<float> Dispersion = new InputSlot<float>();

        [Input(Guid = "0724bf0c-8f97-44de-bf42-6a89b89f1632")]
        public readonly InputSlot<float> CellSize = new InputSlot<float>();

        [Input(Guid = "9defe179-7ea3-4f67-a951-a83b0b173a18")]
        public readonly InputSlot<float> ClampAccelleration = new InputSlot<float>();

        [Input(Guid = "78566405-8dee-4661-9cb7-489c8d322f64")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();
    }
}

