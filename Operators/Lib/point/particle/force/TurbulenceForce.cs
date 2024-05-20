using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3f8376f2_b89a_4ab4_b6dc_a3e8bf88c0a5
{
    public class TurbulenceForce : Instance<TurbulenceForce>
    {

        [Output(Guid = "e5bbe22e-e3f6-4f1f-9db0-fc7632c10639")]
        public readonly Slot<T3.Core.DataTypes.ParticleSystem> Particles = new();

        [Input(Guid = "e27a97ce-3d0f-41a9-93c3-a1691f4029aa")]
        public readonly InputSlot<float> Amount = new();

        [Input(Guid = "f0345217-29f4-48f8-babd-8aed134cb0d5")]
        public readonly InputSlot<float> Frequency = new();

        [Input(Guid = "419b5ec5-8f6d-4c2d-a633-37d125cfcf07")]
        public readonly InputSlot<float> Phase = new();

        [Input(Guid = "56144ddb-9d4b-4e08-9169-7853a767f794")]
        public readonly InputSlot<float> Variation = new();

        [Input(Guid = "dfa6e67f-140b-4f96-bfb7-a8897edce28f")]
        public readonly InputSlot<System.Numerics.Vector3> AmountDistribution = new();

        [Input(Guid = "ebf8276f-2df8-4e70-ba57-30288fb184d1")]
        public readonly InputSlot<bool> UseCurlNoise = new();

        [Input(Guid = "d1ebfcaa-ce47-4064-9169-7afa64f942f5")]
        public readonly InputSlot<T3.Core.Operator.GizmoVisibility> ShowGizmo = new();

        [Input(Guid = "671a04f9-0f40-45ea-a2df-4f06c08d9647")]
        public readonly InputSlot<float> AmountFromVelocity = new();
    }
}

