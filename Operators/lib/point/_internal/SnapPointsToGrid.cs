using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_bc88304a_a2c7_4df9_93d8_b7dfecbce3de
{
    public class SnapPointsToGrid : Instance<SnapPointsToGrid>
    {

        [Output(Guid = "b7bc82a2-f095-490a-91e3-276431d5eb87")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new();

        [Input(Guid = "953a95d0-5226-46bb-80c3-f20b27a32064")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new();

        [Input(Guid = "4d7f1f34-ca1b-43ee-803f-cbc14bcc8679")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "eacc6bf8-1e12-44fb-8541-91ac4a557745")]
        public readonly InputSlot<System.Numerics.Vector3> GridStretch = new();

        [Input(Guid = "04c5ee70-9f9a-4236-8871-772814b8b2ab")]
        public readonly InputSlot<System.Numerics.Vector3> Offset = new();

        [Input(Guid = "a7d80f3e-298d-4eb5-9751-ec432cda4065")]
        public readonly InputSlot<float> SnapFraction = new();

        [Input(Guid = "dc5e2101-e4d5-495a-8291-58aef1876657")]
        public readonly InputSlot<float> FractionBlend = new();

        [Input(Guid = "bf7fef5e-a9d5-41e6-8025-0f74a9e07373")]
        public readonly InputSlot<bool> UseWAsWeight = new();
    }
}

