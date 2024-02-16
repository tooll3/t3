using System.Runtime.InteropServices;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.img.fx._
{
	[Guid("cc34a183-3978-4b6b-8ef1-dd8102410816")]
    public class _multiImageFxSetupStatic : Instance<_multiImageFxSetupStatic>
    {
        [Output(Guid = "76b6c677-12db-4404-aff7-ee3391d2d831")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new();

        [Input(Guid = "f6269be3-3331-43a6-91ec-138b47199f3e")]
        public readonly InputSlot<string> ShaderPath = new();

        [Input(Guid = "55126bff-8c94-415d-96dd-3c16e216e663")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageA = new();

        [Input(Guid = "0bb90f8d-88c9-4a99-b44f-f284b505c65b")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageB = new();

        [Input(Guid = "2929c4c9-6d6a-47b7-b80e-d7a1f90b6945")]
        public readonly MultiInputSlot<float> FloatParams = new();

        [Input(Guid = "9851a909-a9fd-4feb-a8bd-48846cea8461")]
        public readonly InputSlot<Int2> Resolution = new();

        [Input(Guid = "6022a4b0-c6fc-49a3-811a-179dcceb8b44")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new();

        [Input(Guid = "e31b78eb-940b-41df-93fa-d0c1c9f864f4")]
        public readonly InputSlot<bool> GenerateMips = new();

        [Input(Guid = "35f3b28b-2210-4f27-813a-5c857940d09c")]
        public readonly InputSlot<SharpDX.DXGI.Format> TextureFormat = new InputSlot<SharpDX.DXGI.Format>();
    }
}

