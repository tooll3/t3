using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e012c92e_3278_4e4b_9e60_44ccb70648ce
{
    public class GltfPlayground : Instance<GltfPlayground>
    {
        [Output(Guid = "eab0ad64-cfe0-4088-9e3e-9ebdd2b954fd")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

