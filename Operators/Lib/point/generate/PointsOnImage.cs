using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.point.generate
{
	[Guid("722e79cc-47bc-42cc-8fce-2e06f36f8caa")]
    public class PointsOnImage : Instance<PointsOnImage>
    {
        [Output(Guid = "7c8567c9-1456-4040-ad43-4cc8a96efbaf")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutputPoints = new();

        [Output(Guid = "b4aec665-f16d-4345-9016-1e09113ecbb0")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> OutputTexture = new();


        [Input(Guid = "5c7e5e27-2eb8-4933-97cb-fc49d576d625")]
        public readonly InputSlot<int> Count = new();

        [Input(Guid = "065bb5be-e5ee-4ed6-8521-a0969fcb6f4f")]
        public readonly InputSlot<bool> IsEnabled = new();

        [Input(Guid = "5184f2ec-4f91-4dd2-9872-d9ad8d4e5d92")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new();

        [Input(Guid = "6a93d3ee-bf8c-4f4a-9582-62ee1dd752ed")]
        public readonly InputSlot<float> Seed = new();
    }
}

