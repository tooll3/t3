using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Operators.Types.user.pixtur
{
    [Guid("75bd09ea-ce64-4cc5-b718-e42d6b4e4079")]
    public class PixiShow : Instance<PixiShow>
    {
        [Output(Guid = "953d96d1-19ae-4698-8386-192695dd2951")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

