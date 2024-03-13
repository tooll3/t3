using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f752448b_0de6_4791_bee0_4d62d9881d39
{
    public class DeformMesh2 : Instance<DeformMesh2>
    {

        [Output(Guid = "d51b4ef7-a229-4800-86fd-25c865266cec")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new();

        [Input(Guid = "f3aea673-f7e7-4642-8b60-e28662e2dfeb")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new();

        [Input(Guid = "d65191ce-951e-484a-838a-0389357f94b6")]
        public readonly InputSlot<float> Amount = new();

        [Input(Guid = "46900058-fe23-4e92-a344-400b5ee7db8f")]
        public readonly InputSlot<float> Frequency = new();

        [Input(Guid = "241766a4-d006-4f3b-bf5e-ca966622838b")]
        public readonly InputSlot<float> Phase = new();

        [Input(Guid = "5f368e1d-562b-4502-8ce1-d95a943eac0a")]
        public readonly InputSlot<float> Variation = new();

        [Input(Guid = "3d2cc00f-0277-48e0-be78-f7a66e92b4f9")]
        public readonly InputSlot<System.Numerics.Vector3> AmountDistribution = new();

        [Input(Guid = "fb8ac0a2-59fd-4b25-95d6-0b66561dea27")]
        public readonly InputSlot<float> RotationLookupDistance = new();

        [Input(Guid = "2fd9cbb7-9aad-43d1-8d3f-c703390cdcc1")]
        public readonly InputSlot<float> UseWAsWeight = new();

        [Input(Guid = "332a128a-a7da-4987-aacc-6309cc489b90", MappedType = typeof(Spaces))]
        public readonly InputSlot<int> Space = new();

        [Input(Guid = "198ed1f3-51c8-49b5-a04d-f9fe12fa47fa", MappedType = typeof(Directions))]
        public readonly InputSlot<int> Direction = new();
        
        [Input(Guid = "297d6290-1962-4345-aeb4-b803e3e99f44")]
        public readonly InputSlot<float> OffsetDirection = new();

        [Input(Guid = "6333433f-7f36-4490-8f52-f674450f6564")]
        public readonly InputSlot<bool> UseVertexSelection = new();

        [Input(Guid = "36004b80-026f-416b-968c-ba0ad4773e9d")]
        public readonly InputSlot<int> TwistAxis = new InputSlot<int>();

        
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

