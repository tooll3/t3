using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cb28de70_6111_4f64_b6ee_f52b1a35b44a
{
    public class _OldTvGlitch : Instance<_OldTvGlitch>
    {
        [Output(Guid = "0da718da-11a9-4d48-8a39-2097a5a8c2a8")]
        public readonly Slot<Texture2D> Output = new();


        [Input(Guid = "a9b30734-f128-493e-a35e-175b4f109e6e")]
        public readonly InputSlot<Texture2D> Image = new();

        [Input(Guid = "63b6adf3-e245-4145-acec-fdea95564498")]
        public readonly InputSlot<float> Amount = new();

    }
}

