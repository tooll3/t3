using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("87f959cc-4642-4075-a374-d566e1a48d11")]
    public class LookTest11 : Instance<LookTest11>
    {

        [Output(Guid = "a8b3db9a-0dfb-4406-9484-37b32635b6b3")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

