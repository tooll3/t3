using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_bc88304a_a2c7_4df9_93d8_b7dfecbce3de
{
    public class _FloorPointsToGird : Instance<_FloorPointsToGird>
    {

        [Output(Guid = "b7bc82a2-f095-490a-91e3-276431d5eb87")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> Output = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "953a95d0-5226-46bb-80c3-f20b27a32064")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "eacc6bf8-1e12-44fb-8541-91ac4a557745")]
        public readonly InputSlot<System.Numerics.Vector3> Direction = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "4d7f1f34-ca1b-43ee-803f-cbc14bcc8679")]
        public readonly InputSlot<float> Distance = new InputSlot<float>();
    }
}

