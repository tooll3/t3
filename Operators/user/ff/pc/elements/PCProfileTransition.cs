using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.ff.pc.elements
{
	[Guid("a85870e1-6b18-40db-96e2-076536d5a521")]
    public class PCProfileTransition : Instance<PCProfileTransition>
    {
        [Output(Guid = "df1297ac-fd7a-44ae-bf18-c37b6a0c8c4d")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "1e9a70d7-a85a-4b3d-bea1-c0c7c5e8f09d")]
        public readonly InputSlot<string> ProfileImage = new();

        [Input(Guid = "6d1dd4e7-560e-4374-adf1-8e8245070cda")]
        public readonly InputSlot<float> Transition = new();

        [Input(Guid = "a38f3325-346f-43bb-a430-fc2af3b4d0bd")]
        public readonly InputSlot<float> Scale = new();

        [Input(Guid = "7bbfb8c2-082b-4aba-beb5-a4aeb99c7c32")]
        public readonly InputSlot<System.Numerics.Vector3> Offset = new();


    }
}

