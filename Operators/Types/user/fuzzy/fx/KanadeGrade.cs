using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0309e746_c356_4c7b_af05_93136a2607de
{
    public class KanadeGrade : Instance<KanadeGrade>
    {
        [Output(Guid = "4969429f-c7f6-441e-94ab-2a5a12e4cb11")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "93227e69-35c7-4db6-bc6e-e655f2f8226a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "394c0b8a-0d6e-4740-bd33-28c651b1471d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image2 = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "a1eba7f5-e2f1-46cb-8982-ae9cb1b7531b")]
        public readonly InputSlot<float> Lod = new InputSlot<float>();
    }
}

