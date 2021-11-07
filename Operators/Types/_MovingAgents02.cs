using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4edc34ed_36f4_4f24_837f_4cc5696b2baa
{
    public class _MovingAgents02 : Instance<_MovingAgents02>
    {
        [Output(Guid = "fc65f025-f050-403d-9fd9-097a7cc676ca")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();

        [Input(Guid = "f333dbce-cc54-43ba-99b0-065426820f36")]
        public readonly InputSlot<float> RestoreLayout = new InputSlot<float>();

        [Input(Guid = "8ba49b49-8eee-461b-acd8-ba6bc21ba866")]
        public readonly InputSlot<bool> RestoreLayoutEnabled = new InputSlot<bool>();

        [Input(Guid = "3b2e07ad-8218-4740-9e3e-17949ed5fca6")]
        public readonly InputSlot<bool> ShowAgents = new InputSlot<bool>();

        [Input(Guid = "693957bc-5364-404d-84cd-0248fc609eca")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "351eb848-829d-48cb-96c3-16c00366d34f")]
        public readonly InputSlot<int> AgentCount = new InputSlot<int>();

        [Input(Guid = "2b19f72d-db37-4e02-a780-2bb82adb1b54")]
        public readonly InputSlot<int> ComputeSteps = new InputSlot<int>();

        [Input(Guid = "ae4938aa-43c2-443b-8ace-9dbd2045b448")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> BreedsBuffer = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "9c6960f3-ac8d-4ab7-a34e-e24a6217b092")]
        public readonly InputSlot<System.Numerics.Vector4> DecayRatio = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "ee393179-a8a6-4e62-979b-c906244ebb2e")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> EffectTexture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "ad6beaae-4d4b-4f3b-ad7b-7d6389789610")]
        public readonly InputSlot<SharpDX.Size2> BlockCount = new InputSlot<SharpDX.Size2>();


    }
}

