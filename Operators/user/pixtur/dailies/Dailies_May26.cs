using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.dailies
{
	[Guid("b138fb48-9c1f-429f-bdbf-4693d84ef0e2")]
    public class Dailies_May26 : Instance<Dailies_May26>
    {
        [Output(Guid = "92183499-00fe-4d87-829e-66c922106286")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

