using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Lib.img.adjust
{
    [Guid("1e29f81b-0c05-4784-b3ac-c475ce510159")]
    public class MakeTileableImage : Instance<MakeTileableImage>
    {

        [Output(Guid = "970d1691-47f4-43ce-937d-cad6a9c13922")]
        public readonly Slot<Texture2D> Selected = new Slot<Texture2D>();

        [Input(Guid = "f09d6911-dd5f-4dac-a475-3ea291f04dfe")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageA = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "e0f2826f-867d-461b-b1ec-95d30d592539")]
        public readonly InputSlot<float> EdgeFallOff = new InputSlot<float>();

        [Input(Guid = "0cf0b6ec-93e4-4e08-939c-5588267c3991")]
        public readonly InputSlot<int> TilingMode = new InputSlot<int>();

    }
}

