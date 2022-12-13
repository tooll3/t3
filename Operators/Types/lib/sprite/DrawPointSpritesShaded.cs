using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Operators.Types.Id_fd9bffd3_5c57_462f_8761_85f94c5a629b;

namespace T3.Operators.Types.Id_122cbf32_b3e5_4db7_b18d_f2af5b10419c
{
    public class DrawPointSpritesShaded : Instance<DrawPointSpritesShaded>
    {
        [Output(Guid = "0ac5d7d5-e127-4464-9910-82deb4781c91")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "a305b4c3-fc34-4ccc-bff6-ab0eab58d768")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "a49cf849-802f-47fe-9e6c-7f53611d7a41")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Sprites = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "1e2dbb8c-c164-49b3-b96a-b80655d5dcce")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "62dd6e4f-5cf5-4bd9-9683-8b9ed5d423f6")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "3602d9c6-6477-4a29-af63-2fb12c7efbb6")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "08fb9c79-2672-4ece-89bc-6f05e07592d7")]
        public readonly InputSlot<float> AlphaCutOff = new InputSlot<float>();

        [Input(Guid = "e7cd2998-cd5a-494e-8669-b68f59fba257")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();

        [Input(Guid = "5e39b0d4-a268-4022-bfc1-1fdc9a98b48c", MappedType = typeof(PickBlendMode.BlendModes))]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "9324404a-a4e3-46cf-b79a-722c6ab46fff")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> Culling = new InputSlot<SharpDX.Direct3D11.CullMode>();
        
        private enum TextureModes
        {
            RelativeStartEnd,
            StartRepeat,
            Tile,
            UseW,
        }
    }
}

