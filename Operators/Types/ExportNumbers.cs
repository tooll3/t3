using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7a415cae_8e31_46da_826a_c3cb676b2fb0
{
    public class ExportNumbers : Instance<ExportNumbers>
    {
        [Output(Guid = "5572379d-f1a6-4d64-82cd-050fd2ca96d6")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();
    }
}

