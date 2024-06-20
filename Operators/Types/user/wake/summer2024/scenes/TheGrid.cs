using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1bcfb192_cfd4_4ab3_a3f5_9405ec364f60
{
    public class TheGrid : Instance<TheGrid>
    {
        [Output(Guid = "2f5d61ae-640c-4479-915a-8bf732233f01")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "0c49110d-f118-4fd1-a01b-cbfd0ff2eb91")]
        public readonly InputSlot<bool> BreakBeats_1x = new InputSlot<bool>();

        [Input(Guid = "026a1520-48fe-44e0-b6a3-54608c1465e5")]
        public readonly InputSlot<bool> Clamps = new InputSlot<bool>();

        [Input(Guid = "338ef118-af28-4a89-bba2-a6f74e8aedeb")]
        public readonly InputSlot<bool> Hihats_16x = new InputSlot<bool>();

        [Input(Guid = "88e6e8ca-88de-4046-95d5-eea9d4212e2c")]
        public readonly InputSlot<bool> ArpsDupDupDup_4x = new InputSlot<bool>();

        [Input(Guid = "b2817a74-040b-4889-8299-f480152e406b")]
        public readonly InputSlot<bool> VoiceOha_1x = new InputSlot<bool>();

        [Input(Guid = "a3194d6c-028a-4938-ba66-5f6a36e1b61c")]
        public readonly InputSlot<bool> FastSynth_16x = new InputSlot<bool>();

        [Input(Guid = "c31692ff-6a36-43b2-a9fc-2f4fe86194ea")]
        public readonly InputSlot<bool> MainArps2 = new InputSlot<bool>();

        [Input(Guid = "3fd5f6bb-6691-4177-9e60-4a3f00dadb92")]
        public readonly InputSlot<bool> FinalMelody = new InputSlot<bool>();


    }
}

