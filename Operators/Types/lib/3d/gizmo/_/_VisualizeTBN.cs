using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dd353ac7_1f11_4dd6_aff5_5c557c695512
{
    public class _VisualizeTBN : Instance<_VisualizeTBN>
    {
        [Output(Guid = "82fc9f76-6a6d-4464-a94d-e28a06d82205")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "c1f85fa0-c66c-4c7b-b58f-3f9c6375fa3f")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new();

        [Input(Guid = "d9287fce-b451-4d5e-83d8-fc8c9d39b9a8")]
        public readonly InputSlot<float> Length = new();

        [Input(Guid = "d1cfa06d-a4bd-4975-90d4-5aa1ca39dc39")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> Visibility = new();

    }
}

