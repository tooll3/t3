using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("4bc2ca61-54c3-4a6b-a8ae-619a79395870")]
    public class LookTest03 : Instance<LookTest03>
    {

        [Output(Guid = "afb72dc5-df44-4ae5-84cc-b14d94be5d88")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> output2D = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

