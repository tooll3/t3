using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.fx
{
	[Guid("064c1f38-8b6d-44f0-aae3-32dd3916e2e9")]
    public class SticksFlowing : Instance<SticksFlowing>
    {
        [Output(Guid = "65766fa1-21a3-45c6-917d-44322b61045d")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Input(Guid = "dd68faab-fd9c-4939-bba2-388249965678")]
        public readonly InputSlot<Texture2D> Image = new();

        [Input(Guid = "9dc910c7-810f-4787-946c-d0cc811d294d")]
        public readonly InputSlot<float> Scale = new();
    }
}

