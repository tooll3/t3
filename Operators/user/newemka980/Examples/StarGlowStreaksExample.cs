using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.newemka980.Examples
{
	[Guid("4fdd6f40-c099-4caa-a05b-67c8a45961bb")]
    public class StarGlowStreaksExample : Instance<StarGlowStreaksExample>
    {
        [Output(Guid = "0d021020-08c0-46af-b3fe-f4627461322f")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

