using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_78b13039_2abe_41c0_b6b1_16c3e717efb6
{
    public class Boids01 : Instance<Boids01>
    {
        [Output(Guid = "edf11d52-fdf1-4ddf-85af-18d07cf83642")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        [Input(Guid = "36c096f7-54e1-4ff5-9a5c-3fd3004966f3", MappedType = typeof(FxModes))]
        public readonly InputSlot<int> Index = new InputSlot<int>();

        private enum FxModes
        {
            Blob,
            Text,
            Noise,
        }

    }
}

