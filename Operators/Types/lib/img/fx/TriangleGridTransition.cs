using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_da8ebc61_87cf_44ff_888e_994c8628ddb7
{
    public class TriangleGridTransition : Instance<TriangleGridTransition>
    {
        [Output(Guid = "64932d5a-de45-49c2-8ce2-fcde79d6627c")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "5a5bed81-c48d-43dd-a6b0-fc7964f90f2d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "36a9b777-0201-4cec-9c6f-d5ec09e92b3a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageB = new();

        [Input(Guid = "846f8b1e-e6bb-4e6a-a42c-f83636fd4b6f")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new();

        [Input(Guid = "e97a432e-4996-41b0-8ae8-10c9f2d15bc4")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "0c6065b9-eb20-4a37-9d70-419d1c8912a6")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new();

        [Input(Guid = "6164020b-3059-4ce5-8a8f-a56150b8a6be")]
        public readonly InputSlot<float> Divisions = new();

        [Input(Guid = "8c652498-33c7-4faa-a8c0-3f3d296474c8")]
        public readonly InputSlot<float> LineThickness = new();

        [Input(Guid = "659de0cb-82d8-45a9-b99d-748b3c56e8e8")]
        public readonly InputSlot<float> MixOriginal = new();

        [Input(Guid = "dab4e3df-aaa6-411c-83f6-e4c730b740ce")]
        public readonly InputSlot<float> Rotation = new();

        [Input(Guid = "82a572d3-a39f-4ab3-bc17-804d3098d1c1")]
        public readonly InputSlot<float> EffectWidth = new();

        [Input(Guid = "8d4e1265-8479-4525-adbc-fe5c31d9984a")]
        public readonly InputSlot<System.Numerics.Vector2> EffectCenter = new();

        [Input(Guid = "5baee095-f7fe-4a07-a01d-cbb2f3c2f3bc")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "d99d5c03-2563-41e6-9bfe-bd61a7fed37d")]
        public readonly InputSlot<float> Scatter = new();
    }
}

