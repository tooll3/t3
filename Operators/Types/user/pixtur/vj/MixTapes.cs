using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b48e2f43_e87f_4cba_ab86_078e7cdd7444
{
    public class MixTapes : Instance<MixTapes>
    {
        [Output(Guid = "102712cc-75ec-4d1b-b935-2cee9a513745")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Input(Guid = "813f0932-56ab-447d-bd80-370240d2454c", MappedType = typeof(Modes))]
        public readonly InputSlot<int> EffectMode = new();


        private enum Modes {
            Kaleidoscope,
            EdgeRepeat,
            Mirror,
        }
    }
}

