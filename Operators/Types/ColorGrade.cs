using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_42d86738_d644_47c8_ab92_cc426d958e51
{
    public class ColorGrade : Instance<ColorGrade>
    {
        [Output(Guid = "1680781d-af5e-4b77-beb6-3e4a12d73d59")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        
        [Input(Guid = "777B2C27-A3C8-40D0-A196-80A08AF51296")]
        public readonly InputSlot<Texture2D> Texture2d = new InputSlot<Texture2D>();

        [Input(Guid = "4DC44A7B-FE7C-4807-AAAA-53FB553DE017")]
        public readonly InputSlot<System.Numerics.Vector4> Gain = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "BE4DC864-A5F9-4356-91F9-58DE8056A3A8")]
        public readonly InputSlot<System.Numerics.Vector4> Gamma = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "E8CC8A26-313E-4399-B800-901019BBAA78")]
        public readonly InputSlot<System.Numerics.Vector4> Lift = new InputSlot<System.Numerics.Vector4>();
        
        [Input(Guid = "16231DE9-2E85-4A9A-A2D1-99DFAC18A0F6")]
        public readonly InputSlot<float> PreSaturate = new InputSlot<float>();

    }
}