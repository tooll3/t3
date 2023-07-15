using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c276f4eb_f19c_405b_b247_3db159677571
{
    public class CombineMeshes : Instance<CombineMeshes>
    {

        [Output(Guid = "a1303162-1a8e-4dfc-bcb9-644265530742")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> CombinedMesh = new Slot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "815b313e-e661-43db-b788-54cf824c0d8a")]
        public readonly MultiInputSlot<T3.Core.DataTypes.MeshBuffers> Meshes = new MultiInputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "c0bf8ff1-e578-41c2-b4df-40359cb48609")]
        public readonly InputSlot<bool> IsEnabled = new InputSlot<bool>();
    }
}

