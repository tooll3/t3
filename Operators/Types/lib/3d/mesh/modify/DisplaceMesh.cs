using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_79c01289_f3a9_4bea_8e95_a6b5f89b752d
{
    public class DisplaceMesh : Instance<DisplaceMesh>
    {

        [Output(Guid = "d2e47ad5-8830-4e55-97f0-302358b46358")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new();

        [Input(Guid = "f6726946-8ba0-4753-b37a-805c9246d236")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "bbb330cb-6f13-445a-8e7c-c5610380f1ff", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "ea2df434-7963-4266-bef3-8f71ccfa95c0")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "84c2b0ee-c67d-4eab-88ea-fd95c332da07")]
        public readonly InputSlot<System.Numerics.Vector3> AmountDistribution = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "d401f34b-bcea-4374-8a67-25cfc7938593")]
        public readonly InputSlot<System.Numerics.Vector3> Offset = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "0211ebf0-d4b6-45aa-8c75-6e745941aa93")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "c7e1c790-8e52-4064-9ffb-c0b8c2a50320")]
        public readonly InputSlot<System.Numerics.Vector2> ScaleUV = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "7c6fa9b0-1c70-4865-8f01-d22c41ff9b6b")]
        public readonly InputSlot<bool> UseVertexSelection = new InputSlot<bool>();

        [Input(Guid = "ec50aedf-5c2d-4f32-ad7e-205dd435e1fd")]
        public readonly InputSlot<bool> RecomputeNormals_ = new InputSlot<bool>();
        
        private enum Modes
        {
            Surface,
            Surface_XYZ,
            World_XYZ,
        }
    }
}

