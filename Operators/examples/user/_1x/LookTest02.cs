using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("701f78b9-5b1a-4332-ab5e-89bcb1f520ce")]
    public class LookTest02 : Instance<LookTest02>
    {

        [Output(Guid = "1f530af1-4348-4000-b748-1ec1cbfcfb53")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> output2D = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

