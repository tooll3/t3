using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_014b8d6f_c7f2_43b5_84a8_033356e440ef
{
    public class RasterExample : Instance<RasterExample>
    {
        [Output(Guid = "8eedd2e2-8806-4e97-9ca2-ec6d881e62fc")]
        public readonly Slot<Texture2D> Output = new();

    }
}

