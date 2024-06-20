using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.img.generate
{
	[Guid("ba059fe1-3397-4950-9ddd-e328f0c2e0bd")]
    public class VoronoiCells : Instance<VoronoiCells>
    {
        [Output(Guid = "3c91677f-ceb1-4c14-9eda-90ccb70f12a1")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Input(Guid = "f5bad014-1895-4490-8c00-4c775db54716")]
        public readonly InputSlot<Texture2D> Image = new InputSlot<Texture2D>();

        [Input(Guid = "fd554dbb-113f-4722-af28-4e1ec39852cc")]
        public readonly InputSlot<System.Numerics.Vector4> EdgeColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "c81900c4-3478-4a53-b6e6-4e9a09c81a91")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "b0e1cd04-9377-4ab0-9c4e-8780f4efa271")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "7328bfec-fad5-4262-9c10-fad57bd39e2e")]
        public readonly InputSlot<float> EdgeWidth = new InputSlot<float>();

        [Input(Guid = "2bd55eb6-8bca-4f34-bc0e-d1223f47b410")]
        public readonly InputSlot<bool> Animated = new InputSlot<bool>();

        [Input(Guid = "485ea866-3764-402b-8bb5-ade85efde0c1")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> Resolution = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

        [Input(Guid = "29791375-afb4-4612-9e44-a60eed50ea9b")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();
    }
}

