using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cc34a183_3978_4b6b_8ef1_dd8102410816
{
    public class _multiImageFxSetupStatic : Instance<_multiImageFxSetupStatic>
    {
        [Output(Guid = "76b6c677-12db-4404-aff7-ee3391d2d831")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "f6269be3-3331-43a6-91ec-138b47199f3e")]
        public readonly InputSlot<string> ShaderPath = new InputSlot<string>();

        [Input(Guid = "55126bff-8c94-415d-96dd-3c16e216e663")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageA = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "0bb90f8d-88c9-4a99-b44f-f284b505c65b")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> ImageB = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "2929c4c9-6d6a-47b7-b80e-d7a1f90b6945")]
        public readonly MultiInputSlot<float> FloatParams = new MultiInputSlot<float>();

        [Input(Guid = "9851a909-a9fd-4feb-a8bd-48846cea8461")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "6022a4b0-c6fc-49a3-811a-179dcceb8b44")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new InputSlot<SharpDX.Direct3D11.TextureAddressMode>();

        [Input(Guid = "e31b78eb-940b-41df-93fa-d0c1c9f864f4")]
        public readonly InputSlot<bool> GenerateMips = new InputSlot<bool>();
    }
}

