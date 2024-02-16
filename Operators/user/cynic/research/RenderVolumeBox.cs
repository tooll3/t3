using System.Runtime.InteropServices;
using System.Numerics;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.cynic.research
{
	[Guid("74107b72-28d8-4350-9748-01d001e5f033")]
    public class RenderVolumeBox : Instance<RenderVolumeBox>
    {
        [Output(Guid = "da0d2f20-f771-4dc9-9734-57f230e073ec")]
        public readonly Slot<Command> Output = new();


        [Input(Guid = "2b5ee8f6-80c0-4143-b79e-3f7c7d07e4ed")]
        public readonly InputSlot<Vector4> Value = new();

        [Input(Guid = "e7724445-1728-459d-bf1e-2d13063d5c97")]
        public readonly InputSlot<System.Numerics.Vector3> Size = new();

        [Input(Guid = "80550ecb-6cc3-4cc6-8196-83490243d325")]
        public readonly InputSlot<T3.Core.DataTypes.Texture3dWithViews> VolumeData = new();

    }
}

