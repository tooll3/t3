using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("f1950578-44d1-4c02-b289-804853b6cc56")]
    public class LookTest10 : Instance<LookTest10>
    {

        [Output(Guid = "5dbef9ad-a109-460c-bfee-2afd59ecd0d6")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

