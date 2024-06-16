using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_47da8125_359e_40b7_8cf6_1880c51147f6
{
    public class TheTruth : Instance<TheTruth>
    {
        [Output(Guid = "fffca69d-5975-46ac-ae01-855b354a2e57")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "bda603a7-f97f-4c4f-87e1-65f5404863b9")]
        public readonly InputSlot<bool> HitNote = new InputSlot<bool>();

        [Input(Guid = "a9fe4dde-361c-4932-a815-18a4f23a2b60")]
        public readonly InputSlot<bool> Hit2 = new InputSlot<bool>();

        [Input(Guid = "944f3b8b-6aed-4fa9-aaf5-9fe3fdbcc008")]
        public readonly InputSlot<float> NoteValue = new InputSlot<float>();

        [Input(Guid = "e7086e32-0c89-4772-8325-eb254444dae5")]
        public readonly InputSlot<float> SlowHit = new InputSlot<float>();

        [Input(Guid = "1f5f95be-770c-4ef0-8232-d9596337937e")]
        public readonly InputSlot<bool> VerySlowHit = new InputSlot<bool>();


    }
}

