using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.projects.Mappi2
{
    [Guid("d89fbdf2-bf94-43c4-99f8-d25182799587")]
    public class iMapp24 : Instance<iMapp24>
    {
        [Output(Guid = "c5a1d5ca-8157-4194-a695-1bc200228564")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

