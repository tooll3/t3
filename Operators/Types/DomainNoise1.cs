using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_35587846_962f_4baa_afcf_2c82ff9f8402
{
    public class DomainNoise1 : Instance<DomainNoise1>
    {
        [Output(Guid = "ebe9fd53-d38b-47d5-a5ec-58565d3ddc78")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "132ed523-4e55-474e-b27e-3840944f8afd")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "ea86cf03-70e5-4c29-93de-fede12dcc63f")]
        public readonly InputSlot<System.Numerics.Vector4> ColorA = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "6fda7185-2593-4c73-beea-7426eb10e3b9")]
        public readonly InputSlot<System.Numerics.Vector4> ColorB = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "fecd213f-8473-4c14-8d46-defcc071d347")]
        public readonly InputSlot<System.Numerics.Vector2> Size = new InputSlot<System.Numerics.Vector2>();
    }
}

