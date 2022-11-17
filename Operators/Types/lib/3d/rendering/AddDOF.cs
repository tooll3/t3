using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;

namespace T3.Operators.Types.Id_53d3eebd_4ead_4965_b26d_10a8bbd48182
{
    public class AddDOF : Instance<AddDOF>
    {
        [Output(Guid = "d4548a06-9efb-422d-a711-f766956983d8")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Output(Guid = "a54cc25b-9ea2-4012-b462-16c565718cf8")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOut = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Output(Guid = "07695aef-ea0d-4cdc-864f-dd2de8a947fb")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Depth = new Slot<SharpDX.Direct3D11.Texture2D>();


        [Input(Guid = "22f5e8db-0b80-47dc-b30b-4bc49d9fad59")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "3655d507-96b3-4ded-9cef-886ea703ca89")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "97b76d02-e735-4e93-88ad-5c9b8766a63c")]
        public readonly InputSlot<float> FocusDistance = new InputSlot<float>();

        [Input(Guid = "1592e94b-a20d-463c-baec-5fb5dfa85532")]
        public readonly InputSlot<System.Numerics.Vector4> BackgroundColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "493c40f0-21e6-466b-afc2-eff570229c86")]
        public readonly InputSlot<int> MaxSamples = new InputSlot<int>();

        [Input(Guid = "40de51d8-91dd-461d-a7be-d4096313eec2")]
        public readonly InputSlot<System.Numerics.Vector2> NearFarRange = new InputSlot<System.Numerics.Vector2>();

    }
}

