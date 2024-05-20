using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("6a137bd1-ad44-4552-a653-d5a42bf9b4f3")]
    public class LookTest05 : Instance<LookTest05>
    {

        [Output(Guid = "b06d32a3-5d59-4c9d-b0e9-7207b32dacb9")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

