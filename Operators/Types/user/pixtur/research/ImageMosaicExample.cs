using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_56238f0c_bb4c_4883_ab13_80a64887ccd2
{
    public class ImageMosaicExample : Instance<ImageMosaicExample>
    {

        [Output(Guid = "d2ad8648-a738-4fed-a439-fbfbf18f0293")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Input(Guid = "4d3108c8-9b64-41e8-9550-880d59f411f9")]
        public readonly InputSlot<string> FolderPath = new();


    }
}

