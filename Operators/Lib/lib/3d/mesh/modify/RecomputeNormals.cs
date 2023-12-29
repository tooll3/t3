using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_68e0d0cb_1e57_4e9c_9f22_bd7927ddb4c5
{
    public class RecomputeNormals : Instance<RecomputeNormals>
    {

        [Output(Guid = "69a94ae6-21f3-4c04-bb7d-98fb469463bb")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new();

        [Input(Guid = "b55aeb9b-5286-476a-b8f0-86cb96e41310")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new();

        [Input(Guid = "fd3f8225-3d33-40d3-af15-ae768e2c67ad")]
        public readonly InputSlot<bool> RecomputeIndices = new();

        
        private enum Spaces
        {
            PointSpace,
            ObjectSpace,
            WorldSpace,
        }
        
        private enum Directions
        {
            WorldSpace,
            SurfaceNormal,
        }
    }
}

