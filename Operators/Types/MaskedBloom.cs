using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d40966c3_2369_40f2_8202_e5c8ab6d9cc0
{
    public class MaskedBloom : Instance<MaskedBloom>
    {
        [Output(Guid = "8d199a8d-b02e-4fa2-8f7d-b156e4302fe3")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();


        [Input(Guid = "29f6bc05-de55-4336-a275-f06b835c66f8")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "fa5bb047-7466-4d68-9977-7a86815ca0f2")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "25091e3d-36ef-4892-965c-b7d3c983da22")]
        public readonly InputSlot<float> Samples = new InputSlot<float>();

        [Input(Guid = "1bfb5c46-a1dd-41fe-aa6b-96e3d602bc82")]
        public readonly InputSlot<float> Offset = new InputSlot<float>();

        [Input(Guid = "9e40abb3-ea45-4c14-a427-b5379ef56daf")]
        public readonly InputSlot<float> Glow = new InputSlot<float>();

        [Input(Guid = "4837051f-033c-4e9e-9d1c-0fe85c1467cb")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Mask = new InputSlot<SharpDX.Direct3D11.Texture2D>();
    }
}

