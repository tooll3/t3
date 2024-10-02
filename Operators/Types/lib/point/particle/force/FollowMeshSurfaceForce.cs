using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_26bb382c_5e3d_49ae_b17e_5bd49b083d9a
{
    public class FollowMeshSurfaceForce : Instance<FollowMeshSurfaceForce>
    {
        [Output(Guid = "390a17cf-c8df-47c2-baa8-cd4f3aff658f")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> Particles = new();

        [Input(Guid = "a962fea7-b59f-49f4-83f0-a7e8b625e57c")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "ac33e215-86cc-4659-bc87-e0d3c2325aa7")]
        public readonly InputSlot<float> RandomizeSpeed = new InputSlot<float>();

        [Input(Guid = "5052523a-9e7b-4ae3-9d9d-ff16e0241cdc")]
        public readonly InputSlot<float> SurfaceDistance = new InputSlot<float>();

        [Input(Guid = "bbd372b8-5a0d-4dd6-a2dd-13049722e1a1")]
        public readonly InputSlot<float> RandomSurfaceDistance = new InputSlot<float>();

        [Input(Guid = "b19de430-98f0-45c8-87c0-67d563ae5b5a")]
        public readonly InputSlot<float> Spin = new InputSlot<float>();

        [Input(Guid = "1951e2f8-29f5-461f-beca-2535b186956a")]
        public readonly InputSlot<float> RandomSpin = new InputSlot<float>();

        [Input(Guid = "235158be-dc6f-45ad-81fa-8b1b2ef0e1bd")]
        public readonly InputSlot<float> RandomPhase = new InputSlot<float>();

        [Input(Guid = "6dada5f7-252c-4af4-8c2f-f5352bbc3597")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> MeshBuffers = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "de70d11c-93cb-494a-9516-c8a5989c2617")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new InputSlot<T3.Core.Operator.GizmoVisibility>();
        
        
        private enum Modes {
            Legacy,
            EncodeInRotation,
        }
    }
}

