using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_557fcca1_5406_4594_9dd2_9122d3b3a1e2
{
    public class EmitPointClound : Instance<EmitPointClound>
    {
        [Output(Guid = "598a616c-5b49-4d6a-9b65-38879d78cc38")]
        public readonly Slot<T3.Core.Command> Command = new Slot<T3.Core.Command>();

        [Input(Guid = "c138ad9e-5625-4ac1-b6f9-9fff7aba523e")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "a014d4ca-001f-4a41-a239-63346652ddd5")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "27ed0afb-b4d3-4212-8ae6-32e1f7bc14cb")]
        public readonly InputSlot<SharpDX.Direct3D11.ShaderResourceView> Data = new InputSlot<SharpDX.Direct3D11.ShaderResourceView>();

        [Input(Guid = "9f98e4a1-a612-4b81-9233-b8f6f34fdd3b")]
        public readonly InputSlot<int> EmitterId = new InputSlot<int>();
    }
}

