using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.MandelbrotFractal
{
    [Guid("27e58fae-2b3d-404e-b9cd-307cb6ad4906")]
    public class MandelbrotFractal : Instance<MandelbrotFractal>
    {
        [Output(Guid = "70703977-c5bb-4e41-9f8b-2e6e8903d434")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new();


        [Input(Guid = "ebae0adf-960c-4cd9-8d2b-532907e51ad3")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "67e309ff-e258-45af-b583-2f86f39de0d3")]
        public readonly InputSlot<float> Phase = new InputSlot<float>();

        [Input(Guid = "443997b5-3b8d-4925-b534-d794b7aafe35")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "fa2fd0f7-45eb-44d9-b1de-cc45903fc2d4")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "4fe49e0c-3a46-4a79-944e-0cfb8d31ebb2")]
        public readonly InputSlot<float> ColorScale = new InputSlot<float>();
    }
}

