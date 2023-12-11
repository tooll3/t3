using System;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_6854e04f_8b60_41b2_a369_ca0b715c4df3
{
    public class CafeWallPattern : Instance<CafeWallPattern>
    {
        [Output(Guid = "cdf70feb-2cb0-44cd-bf4b-38c7af917029")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "2ac6c8b8-ebf5-4653-b390-da008c4be0e6")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "39ae4389-b20d-42b1-b0a4-a9359e3e2f2b")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "635cba46-9c44-4cf9-a03c-9647e13fea83")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "a5e7133e-5ba6-4f01-9a2f-a8fb591aae54")]
        public readonly InputSlot<System.Numerics.Vector4> EdgeColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "78082902-ffb0-47c2-a611-6c1ca8d2a599")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "6c55b1a0-8092-42ec-b143-69d782298e7a")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "f334560f-d745-4a1e-9df6-e0acba043d31")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "6415887f-bae9-4e2c-a5d1-cc09f9d6c6bd")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "c98d3ff9-f4d7-482e-90bc-fe1b32382766")]
        public readonly InputSlot<float> Feather = new InputSlot<float>();

        [Input(Guid = "c1c35717-7ea6-4507-bb7f-bfc827e0446e")]
        public readonly InputSlot<float> Ratio = new InputSlot<float>();

        [Input(Guid = "e023f382-014e-4f37-ab9e-5914a3924071")]
        public readonly InputSlot<float> EdgeWidth = new InputSlot<float>();

        [Input(Guid = "38a04ab5-b0d7-4c69-9647-fc86534892af")]
        public readonly InputSlot<float> RowSwift = new InputSlot<float>();

        [Input(Guid = "0199068d-c826-4b08-9987-8e480ce064a7")]
        public readonly InputSlot<float> RAffects_Ratio = new InputSlot<float>();

        [Input(Guid = "6a34fae1-0c39-4a3a-bc72-1f8fde957b27")]
        public readonly InputSlot<float> GAffects_EdgeWidth = new InputSlot<float>();

        [Input(Guid = "0480f95d-3ab6-4d53-8452-bd79a5438d71")]
        public readonly InputSlot<float> BAffects_RowShift = new InputSlot<float>();

        [Input(Guid = "d3827fcf-ccee-471e-8cbd-1ffd2155be40")]
        public readonly InputSlot<Int2> Resolution = new InputSlot<Int2>();

        [Input(Guid = "9bc1afbb-d9e5-4bc8-afa9-2cbba0215a5a")]
        public readonly InputSlot<float> AmplifyIllusion = new InputSlot<float>();
    }
}

