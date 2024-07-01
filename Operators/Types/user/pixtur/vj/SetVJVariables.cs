using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e14af8a3_8672_4348_af9e_735714c31c92
{
    public class SetVJVariables : Instance<SetVJVariables>
    {

        [Output(Guid = "a8127182-4b8d-4be2-8c50-9ce475d2699d")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output2 = new();

        
        [Input(Guid = "693345bd-0cd8-4dca-9416-42a9bdcbc293")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new();

        [Input(Guid = "83188b7c-4bb7-4884-b6f0-d73fbf7debe1", MappedType = typeof(Devices))]
        public readonly InputSlot<int> MidiDevice = new InputSlot<int>();

        private enum Devices
        {
            ApcMini,
            Apc40MKII,
            MidiMix,
        }
    }
}

