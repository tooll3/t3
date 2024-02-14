using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ba059fe1_3397_4950_9ddd_e328f0c2e0bd
{
    public class VoronoiCells : Instance<VoronoiCells>
    {
        [Output(Guid = "3c91677f-ceb1-4c14-9eda-90ccb70f12a1")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "f5bad014-1895-4490-8c00-4c775db54716")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "fd554dbb-113f-4722-af28-4e1ec39852cc")]
        public readonly InputSlot<System.Numerics.Vector4> EdgeColor = new();

        [Input(Guid = "c81900c4-3478-4a53-b6e6-4e9a09c81a91")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "2bd55eb6-8bca-4f34-bc0e-d1223f47b410")]
        public readonly InputSlot<bool> Inverted = new();

        [Input(Guid = "b0e1cd04-9377-4ab0-9c4e-8780f4efa271")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "485ea866-3764-402b-8bb5-ade85efde0c1")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "55c3fe18-be3c-479b-b9cf-4473c4696f2e")]
        public readonly InputSlot<float> Radius = new();

        [Input(Guid = "7328bfec-fad5-4262-9c10-fad57bd39e2e")]
        public readonly InputSlot<float> LineWidth = new();
    }
}

