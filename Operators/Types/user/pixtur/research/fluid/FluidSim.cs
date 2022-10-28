using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_de8c8149_5b73_40f3_b55d_0fc5031f44ea
{
    public class FluidSim : Instance<FluidSim>
    {
        [Output(Guid = "4807300c-c9c4-4a20-b17c-2bfa59e81105")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();

        [Input(Guid = "2dd6ce17-3f3d-4f94-bd63-60546eddd4ff")]
        public readonly InputSlot<float> RestoreLayout = new InputSlot<float>();

        [Input(Guid = "5234f5b4-64c3-4865-a099-e630e4e69341")]
        public readonly InputSlot<bool> RestoreLayoutEnabled = new InputSlot<bool>();

        [Input(Guid = "5f0b271d-ca80-42cf-a5c7-7a0014c6e467")]
        public readonly InputSlot<bool> ShowAgents = new InputSlot<bool>();

        [Input(Guid = "c103bc4e-41c3-4dfd-823f-ea88225f8d7b")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "5a6aeb37-22e0-4fa1-9973-292b1fa2c6ad")]
        public readonly InputSlot<int> AgentCount = new InputSlot<int>();

        [Input(Guid = "3348b66b-ed36-411e-bdcb-8e2e2a25282b")]
        public readonly InputSlot<int> ComputeSteps = new InputSlot<int>();

        [Input(Guid = "59f17151-012b-4e69-958d-aafc0cde9455")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> BreedsBuffer = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "0489f7db-6ac6-4d2e-9c13-10cef4afc9ef")]
        public readonly InputSlot<System.Numerics.Vector4> DecayRatio = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "4cf61e94-b814-4f41-9f97-7737d929fe8f")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> EffectTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "75a7592e-c481-4ab7-8e36-934039ca83cd")]
        public readonly InputSlot<SharpDX.Size2> BlockCount = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "5ee2f387-5faa-4dae-8e31-19c6c1b0f7c3")]
        public readonly InputSlot<float> AngleLockSteps = new InputSlot<float>();

        [Input(Guid = "a501c949-5a6f-4bb6-ab9c-b8be6b32a1af")]
        public readonly InputSlot<float> AngleLockFactor = new InputSlot<float>();


    }
}

