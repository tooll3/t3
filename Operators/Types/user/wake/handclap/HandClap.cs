using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_6aa4fc3d_c93e_4422_944c_c10d78b36b8f
{
    public class HandClap : Instance<HandClap>
    {
        [Output(Guid = "f44fade0-9411-422a-9108-c288f4c925c5")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

