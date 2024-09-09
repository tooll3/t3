using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_27b0e1af_cb2e_4603_83f9_5c9b042d87e6
{
    public class Blob : Instance<Blob>
    {
        [Output(Guid = "b882de23-5b94-4791-af13-e195211cffb3")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();


        [Input(Guid = "8cc15ea0-074f-40ed-813d-b93f48681094")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "d2b0dd99-c289-4c1b-9335-c29a6b4a6ba3")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new();

        [Input(Guid = "fd05c355-7afa-4af6-9529-d4071d145d3b")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "7daacb43-54de-47d2-afcd-694f6afce59d")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new();

        [Input(Guid = "37da22d0-56ca-444a-9c9d-27a70283b7c0")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new();

        [Input(Guid = "33f31c62-b0ea-42f9-a226-d0f5154731ee")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "f0c128b1-27a1-42e0-a8a4-6fd94d527c05")]
        public readonly InputSlot<float> Feather = new();

        [Input(Guid = "0c49c872-852a-4f15-8cde-f3cda743c28e")]
        public readonly InputSlot<float> FeatherBias = new();

        [Input(Guid = "e45f325d-cf2d-4972-aea6-9545aec66ea7")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "77544b82-e897-4e69-bfaf-e861891d1fd4")]
        public readonly InputSlot<float> Rotate = new();

        [Input(Guid = "89ca8093-8d13-471d-bfb4-613b13bc084d")]
        public readonly InputSlot<bool> GenerateMips = new();

        [Input(Guid = "a9d505ce-2cb1-4911-acc3-c509e1eebc2b", MappedType = typeof(SharedEnums.RgbBlendModes))]
        public readonly InputSlot<int> BlendMode = new();
    }
}

