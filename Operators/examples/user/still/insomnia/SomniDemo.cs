using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f05356b6_3456_4a1b_988c_0c8f89fb4816
{
    public class SomniDemo : Instance<SomniDemo>
    {
        [Output(Guid = "2297f9a6-4c96-45c7-be81-0f7d048428d3")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

