using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a85870e1_6b18_40db_96e2_076536d5a521
{
    public class PCProfileTransition : Instance<PCProfileTransition>
    {
        [Output(Guid = "df1297ac-fd7a-44ae-bf18-c37b6a0c8c4d")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "1e9a70d7-a85a-4b3d-bea1-c0c7c5e8f09d")]
        public readonly InputSlot<string> ProfileImage = new InputSlot<string>();

        [Input(Guid = "6d1dd4e7-560e-4374-adf1-8e8245070cda")]
        public readonly InputSlot<float> Transition = new InputSlot<float>();

        [Input(Guid = "a38f3325-346f-43bb-a430-fc2af3b4d0bd")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

        [Input(Guid = "7bbfb8c2-082b-4aba-beb5-a4aeb99c7c32")]
        public readonly InputSlot<System.Numerics.Vector3> Offset = new InputSlot<System.Numerics.Vector3>();


    }
}

