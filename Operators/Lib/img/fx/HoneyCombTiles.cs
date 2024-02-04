using System.Runtime.InteropServices;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.img.fx
{
	[Guid("ee398291-674c-409b-aab8-9ca843ce6cef")]
    public class HoneyCombTiles : Instance<HoneyCombTiles>
    {
        [Output(Guid = "2c2d62c4-3735-40a4-bc97-eaecf7e4cb7f")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();

        [Input(Guid = "452fb0e1-34c1-4042-9a7c-30bf8dd4cb3c")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "dc8c94e6-97d9-4aa5-9edd-5a8d01e4f7c4")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new();

        [Input(Guid = "165b0ad5-1fde-4f8d-871e-f1498a419887")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new();

        [Input(Guid = "4ba0a4d2-44c8-4c8e-95d1-7b1fbda6975b")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new();

        [Input(Guid = "00ffc593-d62e-4f40-8957-906b444b8f3b")]
        public readonly InputSlot<float> Divisions = new();

        [Input(Guid = "e635d5cd-8250-4722-a11a-ffe27a922f3e")]
        public readonly InputSlot<float> LineThickness = new();

        [Input(Guid = "228b8705-92c7-467b-ab0f-eee60a618c72")]
        public readonly InputSlot<float> MixOriginal = new();

        [Input(Guid = "0c227ab3-3394-40fb-9274-d857a2293d35")]
        public readonly InputSlot<float> Rotation = new();

        [Input(Guid = "03afbb91-a64f-48bc-a306-b378a068aca8")]
        public readonly InputSlot<Int2> Resolution = new();
    }
}

