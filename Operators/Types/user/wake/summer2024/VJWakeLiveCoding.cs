using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d86c172f_bc6d_4bdb_9be8_3ee34b89fcc3
{
    public class VJWakeLiveCoding : Instance<VJWakeLiveCoding>
    {
        [Output(Guid = "b3ceee6d-fad2-44ce-a871-6ce423352986")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

