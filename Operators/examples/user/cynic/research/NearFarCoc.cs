using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.cynic.research
{
	[Guid("5b234754-2b45-46db-becb-86f0bb547608")]
    public class NearFarCoc : Instance<NearFarCoc>
    {
        [Output(Guid = "a0490fe9-eb0a-4fa8-a322-79e61d93e264")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "aa1de9c8-b4fe-4336-8396-0b004880ca1f")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Color = new();

        [Input(Guid = "84df1c1f-b274-4df0-84c8-cbfefabe3b07")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> DepthBuffer = new();

        [Input(Guid = "ffde41de-88a0-4335-9270-fb38604437ed")]
        public readonly InputSlot<float> Near = new();

        [Input(Guid = "58d13b44-59b2-48a3-b5a2-e75591c6ddfa")]
        public readonly InputSlot<float> Far = new();

        [Input(Guid = "6df064b0-0432-448b-a050-919438403667")]
        public readonly InputSlot<float> FocusCenter = new();

        [Input(Guid = "48487fd3-ee5b-4a6a-a3dc-8f46747c4ba3")]
        public readonly InputSlot<float> FocusRange = new();

        [Input(Guid = "5becdd24-fb0b-412b-9211-720af3bd6407")]
        public readonly InputSlot<float> BlurSize = new();

        [Input(Guid = "a69d237c-9960-4bf8-a956-3aabc12c940d")]
        public readonly InputSlot<float> QualityScale = new();

        [Input(Guid = "6e333cc0-7e13-448e-9249-330eda85d752")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> OutputTexture = new();

    }
}

