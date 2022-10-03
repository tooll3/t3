using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_51130e6c_53b2_4294_a3cf_b0fa15584acf
{
    public class EmitFromMesh : Instance<EmitFromMesh>
    {
        [Output(Guid = "5a3b555d-630d-428f-8e6a-c0f70514f85b")]
        public readonly Slot<T3.Core.Command> Command = new Slot<T3.Core.Command>();

        [Input(Guid = "9be529cc-cc76-4b9c-9796-4b9aa70e203f")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "9f258713-3fd8-4eea-be2e-d8db5f2ef50f")]
        public readonly InputSlot<SharpDX.Direct3D11.ShaderResourceView> Data = new InputSlot<SharpDX.Direct3D11.ShaderResourceView>();

        [Input(Guid = "ee425eea-ca35-44ba-abfb-989ef2b6eba3")]
        public readonly InputSlot<int> EmitterId = new InputSlot<int>();

        [Input(Guid = "250a2442-ec83-42dc-a778-9529ab0a0dfd")]
        public readonly InputSlot<int> EmitRate = new InputSlot<int>();

        [Input(Guid = "471e3836-cdb5-44a1-a16c-94090dc970b9")]
        public readonly InputSlot<bool> Emit = new InputSlot<bool>();

        [Input(Guid = "30dbeb68-ddc8-48ca-9d51-d7bca870eae0")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "c43aaa99-9dde-4e7c-9aa9-f551f0a652cb")]
        public readonly InputSlot<float> LifeTime = new InputSlot<float>();

        [Input(Guid = "d52312e1-08f4-4e91-9635-315ad01f11a9")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "e9f5ee44-7f0a-4202-b75d-d2c76d18adcc")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "eccf1221-6775-42e0-8f79-ae5c6c38cb48")]
        public readonly InputSlot<float> Seed = new InputSlot<float>();

        [Input(Guid = "c694ba46-1862-44e6-8d44-664f015f8dc1")]
        public readonly InputSlot<SharpDX.Direct3D11.Buffer> buffer = new InputSlot<SharpDX.Direct3D11.Buffer>();
    }
}

