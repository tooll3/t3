using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_208a86b5_55cc_460a_86e6_2b17da818494
{
    public class TransformMeshUVs : Instance<TransformMeshUVs>
    {

        [Output(Guid = "1030db1a-e5d0-4eac-9f3d-cc1e8d3867c7")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new Slot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "b9e7efdf-98d6-4d5a-94e8-16f38cfe4e55")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "b8409f71-d2e3-4fb3-91dc-abf96b55379f")]
        public readonly InputSlot<System.Numerics.Vector3> Translate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "4b8a067d-8630-485e-b390-1fca7cc06323")]
        public readonly InputSlot<System.Numerics.Vector3> Stretch = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "d2d278d9-7933-49ea-83c2-3566f5a13d1f")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "da73250e-fcf2-4fe9-9a84-a1d139a0390c")]
        public readonly InputSlot<System.Numerics.Vector3> Rotate = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "888017f2-3ac2-464b-ae6f-9f8caf53ba6d")]
        public readonly InputSlot<bool> UseVertexSelection = new InputSlot<bool>();

        
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

