using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_4bf73e3c_1d8c_4007_b155_b0edf00c2e2e
{
    public class TextureMapForceExample2 : Instance<TextureMapForceExample2>
    {
        [Output(Guid = "97420db1-fd37-4f4b-81b2-03146cb542a9")]
        public readonly Slot<Texture2D> TextureOut = new Slot<Texture2D>();


    }
}

