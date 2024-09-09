using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ffd94e5a_bc98_4e70_84d8_cce831e6925f
{
    public class DrawPoints : Instance<DrawPoints>
    {
        [Output(Guid = "b73347d9-9d9f-4929-b9df-e2d6db722856")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "5df18658-ef86-4c0f-8bb4-4ac3fbbf9a33")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "cc442161-e9ca-40ea-be3b-f87189d4e155")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "414c8045-5086-4449-9d9a-03f28c3966b3")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "d0a58cde-d292-4ceb-ba50-6965eb3ee3dd")]
        public readonly InputSlot<bool> UseWForSize = new InputSlot<bool>();

        [Input(Guid = "8fab9161-48d4-43b0-a18f-5942b7071a49", MappedType = typeof(SharedEnums.BlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "3fbad175-6060-40f2-a675-bdae20107698")]
        public readonly InputSlot<float> FadeNearest = new InputSlot<float>();

        [Input(Guid = "814fc516-250f-4383-8f20-c2a358bbe4e1")]
        public readonly InputSlot<bool> EnableZWrite = new InputSlot<bool>();

        [Input(Guid = "7acc95ad-d317-42fc-97f8-85f48d7e516f")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "850e3a32-11ba-4ad2-a2b1-6164f077ddd6")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new InputSlot<SharpDX.Direct3D11.Texture2D>();
    }
}

