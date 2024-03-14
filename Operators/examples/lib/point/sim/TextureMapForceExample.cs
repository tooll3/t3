using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point.sim
{
	[Guid("af0a4265-44aa-49d9-b674-5b7c1937c99a")]
    public class TextureMapForceExample : Instance<TextureMapForceExample>
    {
        [Output(Guid = "c3c883f9-6f5b-4057-bced-62a1f9a09bb1")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

