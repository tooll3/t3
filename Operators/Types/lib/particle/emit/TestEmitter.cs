using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_36cec77e_fd3b_4ec7_ac93_74ee67fb97a2
{
    public class TestEmitter : Instance<TestEmitter>
    {
        [Output(Guid = "1600bf44-d2d4-42d9-a75d-c94a6466309b")]
        public readonly Slot<Command> Command = new Slot<Command>();

        [Input(Guid = "16576e3c-a55e-426e-bf1f-95f18f8648ec")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "cf063078-99d3-421d-80d4-da83aa89a859")]
        public readonly InputSlot<System.Numerics.Vector4> ScatterColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "b7e871b1-49bb-44e1-9a71-203911a42d97")]
        public readonly InputSlot<System.Numerics.Vector4> RandomVelocity = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "0f4f8d27-391e-492f-9075-70a50ed8ee42")]
        public readonly InputSlot<float> Lifetime = new InputSlot<float>();

        [Input(Guid = "60661094-3f20-410a-8387-62e23b98b4be")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "69566d1b-fb5b-45c8-abe9-f03ed18fa968")]
        public readonly MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView> ShaderResources = new MultiInputSlot<SharpDX.Direct3D11.ShaderResourceView>();
    }
}