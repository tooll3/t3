using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.fx
{
	[Guid("bd0f7059-021d-4dd1-8168-01e1e552fb04")]
    public class OpticalFlow2 : Instance<OpticalFlow2>
    {

        [Output(Guid = "0dca196f-bd3b-4cc1-ac5f-929f2a6dfdce")]
        public readonly Slot<Texture2D> Output = new();

        [Output(Guid = "bec5b37d-b007-4bd7-9f5f-dc545820140a")]
        public readonly Slot<Texture2D> OutputRG = new();

        [Input(Guid = "f07bae81-5e2d-4d50-a22c-424434d8bbba")]
        public readonly InputSlot<int> Scale = new();

        [Input(Guid = "7b952ebd-7d63-4b74-8aa2-977c23d0487d")]
        public readonly InputSlot<float> Lod = new();

        [Input(Guid = "e2dfcaa4-6e1f-4eee-9052-f4e036bc39fb")]
        public readonly InputSlot<Texture2D> Image1 = new();

        [Input(Guid = "7cae97b4-f83e-4862-8be4-6ca7142aeb1f")]
        public readonly InputSlot<Texture2D> Image2 = new();

    }
}

