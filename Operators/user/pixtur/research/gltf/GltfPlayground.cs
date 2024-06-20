using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.research.gltf
{
	[Guid("e012c92e-3278-4e4b-9e60-44ccb70648ce")]
    public class GltfPlayground : Instance<GltfPlayground>
    {
        [Output(Guid = "eab0ad64-cfe0-4088-9e3e-9ebdd2b954fd")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

