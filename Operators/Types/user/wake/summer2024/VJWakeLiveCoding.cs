using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.wake.summer2024
{
    [Guid("d86c172f-bc6d-4bdb-9be8-3ee34b89fcc3")]
    public class VJWakeLiveCoding : Instance<VJWakeLiveCoding>
    {
        [Output(Guid = "b3ceee6d-fad2-44ce-a871-6ce423352986")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

