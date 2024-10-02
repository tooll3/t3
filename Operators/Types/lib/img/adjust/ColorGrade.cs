using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_42d86738_d644_47c8_ab92_cc426d958e51
{
    public class ColorGrade : Instance<ColorGrade>
    {
        [Output(Guid = "1680781d-af5e-4b77-beb6-3e4a12d73d59")]
        public readonly Slot<Texture2D> Output = new();

        [Input(Guid = "777b2c27-a3c8-40d0-a196-80a08af51296")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture2d = new();

        [Input(Guid = "16231de9-2e85-4a9a-a2d1-99dfac18a0f6")]
        public readonly InputSlot<float> PreSaturate = new();

        [Input(Guid = "4dc44a7b-fe7c-4807-aaaa-53fb553de017")]
        public readonly InputSlot<System.Numerics.Vector4> Gain = new();

        [Input(Guid = "be4dc864-a5f9-4356-91f9-58de8056a3a8")]
        public readonly InputSlot<System.Numerics.Vector4> Gamma = new();

        [Input(Guid = "e8cc8a26-313e-4399-b800-901019bbaa78")]
        public readonly InputSlot<System.Numerics.Vector4> Lift = new();

        [Input(Guid = "423eb712-f7e3-4402-b841-324a9fc91c54")]
        public readonly InputSlot<System.Numerics.Vector4> VignetteColor = new();

        [Input(Guid = "a0aaadb8-3b39-4a29-b04b-5043ec8bbf42")]
        public readonly InputSlot<float> VignetteRadius = new();

        [Input(Guid = "e94da387-2c81-4ae0-a37e-3141e16c345d")]
        public readonly InputSlot<float> VignetteFeather = new();

        [Input(Guid = "f493d824-0f3e-4b30-838b-59ee0fba755b")]
        public readonly InputSlot<System.Numerics.Vector2> VignetteCenter = new();

        [Input(Guid = "b3e7d147-5f9a-480f-a5da-ea611e5b4805")]
        public readonly InputSlot<bool> GenerateMipmaps = new();

        [Input(Guid = "c73bc2e6-338b-40f9-bfd6-32b6472ff250")]
        public readonly InputSlot<bool> ClampResult = new();

    }
}