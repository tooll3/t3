using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9f43f769_d32a_4f49_92ac_e0be3ba250cf
{
    public class Blend : Instance<Blend>
    {
        [Output(Guid = "536fae14-b814-498c-a6b4-07775de36991")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Input(Guid = "abaa52e9-7d3d-4ae5-89d2-5251f61e5392")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageA = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "6541c1ac-ba84-4a46-a6df-8ab52455c57b")]
        public readonly InputSlot<System.Numerics.Vector4> ColorA = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "c7c524cf-e31e-4bac-8f77-58bd61b337de")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageB = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "70dc133e-800a-4cd0-a159-2cbab4c322cb")]
        public readonly InputSlot<System.Numerics.Vector4> ColorB = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "fc5f1d08-3997-4ba3-ac59-d86e4e501fb0", MappedType = typeof(RgbBlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "cad32967-e91b-4bd1-af09-5fdfdeee630e", MappedType = typeof(AlphaBlendModes))]
        public readonly InputSlot<int> AlphaMode = new InputSlot<int>();

        [Input(Guid = "cdc2cf4b-f788-4cb4-a3f5-8dfaa8bd54b3")]
        public readonly InputSlot<bool> NormalForUpperHalf = new InputSlot<bool>();

        [Input(Guid = "e8923e83-ce48-4b71-a1aa-46b9ff79780b", MappedType = typeof(ScaleModes))]
        public readonly InputSlot<int> ScaleMode = new InputSlot<int>();

        [Input(Guid = "d7b83780-c4be-4337-b1df-0c98310b1926")]
        public readonly InputSlot<bool> GenerateMips = new InputSlot<bool>();

        [Input(Guid = "93e63b73-e572-4bb2-bbbd-11bbffad89e7")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> Resolution = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

        private enum RgbBlendModes
        {
            Normal = 0,
            Screen = 1,
            Multiply = 2,
            Overlay = 3,
            Difference = 4,
            UseImageA_RGB = 5,
            UseImageB_RGB = 6,
            Max = 7,
            Sub = 8,
            MixUsingImageB_A = 9,
        }

        private enum AlphaBlendModes
        {
            Normal = 0,
            Multiply = 1,
            SetToOne = 2,
            UseImageA_Alpha = 3,
            UseImageB_Alpha = 4,
            UseImageA_Brightness = 5,
            UseImageB_Brightness = 6,
            Additive = 7,
            Max = 8,
        }

        private enum ScaleModes
        {
            Stretch = 0,
            Fit = 1,
            Cover = 2,
        }
    }
}