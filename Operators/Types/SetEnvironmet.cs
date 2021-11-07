using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f3b66187_34b2_4018_8380_279f9f296ded
{
    public class SetEnvironmet : Instance<SetEnvironmet>
    {
        [Output(Guid = "1f8cbdfd-ffcd-4604-b4b4-5f1184daf138")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "e4482cab-f20c-4f9f-9179-c64944164509")]
        public readonly InputSlot<Command> SubTree = new InputSlot<Command>();

        [Input(Guid = "5c042a08-74b3-4a6b-a420-2fcfa0fc01ee")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> CubeMap = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "c3c815fa-8672-4d99-99a7-986844f2fc45")]
        public readonly InputSlot<bool> UpdateLive = new InputSlot<bool>();

        [Input(Guid = "71c54c8e-a95f-47e8-b126-0cdaa89ae49b")]
        public readonly InputSlot<float> Exposure = new InputSlot<float>();

    }
}

