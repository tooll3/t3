using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f3b66187_34b2_4018_8380_279f9f296ded
{
    public class SetEnvironment : Instance<SetEnvironment>
    {
        [Output(Guid = "1f8cbdfd-ffcd-4604-b4b4-5f1184daf138")]
        public readonly Slot<Command> Output = new();


        [Input(Guid = "e4482cab-f20c-4f9f-9179-c64944164509")]
        public readonly InputSlot<Command> SubTree = new();

        [Input(Guid = "5c042a08-74b3-4a6b-a420-2fcfa0fc01ee")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> CubeMap = new();

        [Input(Guid = "c3c815fa-8672-4d99-99a7-986844f2fc45")]
        public readonly InputSlot<bool> UpdateLive = new();

        [Input(Guid = "71c54c8e-a95f-47e8-b126-0cdaa89ae49b")]
        public readonly InputSlot<float> Exposure = new();

        [Input(Guid = "4f573afe-8815-4fd3-a655-89ec40bf3c22")]
        public readonly InputSlot<bool> RenderBackground = new();

        [Input(Guid = "96094239-9d82-4a32-bbb0-e9da7f6501da")]
        public readonly InputSlot<float> BackgroundBlur = new();

        [Input(Guid = "650aa9a6-4aa6-4928-be76-3f1f825aa773")]
        public readonly InputSlot<System.Numerics.Vector4> BackgroundColor = new();

        [Input(Guid = "0299761d-7397-4a2f-b591-81fadb404a92")]
        public readonly InputSlot<float> BackgroundDistance = new();

    }
}

