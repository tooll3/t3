using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_42e6319e_669c_4524_8d0d_9416a86afdb3
{
    public class AsciiRender : Instance<AsciiRender>
    {
        [Output(Guid = "b0b6a771-e1a4-4681-a8be-8ed7ac1f66c4")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Input(Guid = "b7d24c9b-ad9e-4ba5-82d9-15414868cdd9")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageA = new();

        [Input(Guid = "9e093ac2-0dc0-4791-bb27-36d1f6ea1c47")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new();

        [Input(Guid = "66509dd9-372b-4351-ad18-d7713d75cc01")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "8c6c08b8-0aa2-492a-b6e7-f49be41b3e6b")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new();

        [Input(Guid = "c7502cc9-bfe4-40d1-9b85-c6f59a15e675")]
        public readonly InputSlot<Int2> FontCharSize = new();

        [Input(Guid = "4623488a-cef2-4aaa-bfea-54e39e0b5653")]
        public readonly InputSlot<float> ScaleFactor = new();

        [Input(Guid = "52f3be7b-c155-430a-97ca-e2cf36631089")]
        public readonly InputSlot<float> Bias = new();

        [Input(Guid = "86bb9127-10fb-47cb-9aee-45be01567810")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "7f35bc61-a27f-4a36-9ecc-b5944791f6f0")]
        public readonly InputSlot<string> FontFilePath = new();

        [Input(Guid = "4a300626-b562-4a5e-bfc5-9ffcd99dc0d5")]
        public readonly InputSlot<string> FilterCharacters = new();

        [Input(Guid = "68801326-950b-4675-8450-56abf64e8518")]
        public readonly InputSlot<float> MixInColors = new();

        [Input(Guid = "011db3c0-4b2b-4040-934a-c52f671b40fd")]
        public readonly InputSlot<float> Randomize = new();

        [Input(Guid = "03d335bb-e4c6-4154-a53b-ab1af64e301f")]
        public readonly InputSlot<bool> GenerateMips = new();

        [Input(Guid = "a842bd72-abb2-4207-8397-7e727aaa6c63")]
        public readonly InputSlot<System.Numerics.Vector2> BiasAndGain = new InputSlot<System.Numerics.Vector2>();

    }
}

