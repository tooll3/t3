using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.projects.Mappi2
{
    [Guid("48e224f0-666b-4121-88e2-e0f7fb00a3da")]
    public class iMappCameraSetup : Instance<iMappCameraSetup>
    {
        [Output(Guid = "d0984f51-0a5d-43fb-b20d-a0e8f33c7fb1")]
        public readonly Slot<Command> Output = new Slot<Command>();
        
        [Output(Guid = "7B95936A-B8FC-4265-B6E4-B4DD4C91C3F6")]
        public readonly Slot<object> CamReference = new ();

        [Input(Guid = "e7d8fd54-447d-4ee4-8711-1441743c30b0")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "4ce1fff5-7a58-462c-9b43-aab973593217")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();

    }
}

