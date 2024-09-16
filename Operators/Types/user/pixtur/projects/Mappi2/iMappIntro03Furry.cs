using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.projects.Mappi2
{
    [Guid("34e5d549-361e-4f4d-a4f3-778d53559079")]
    public class iMappIntro03Furry : Instance<iMappIntro03Furry>
    {
        [Output(Guid = "e0f0ace0-20c0-432c-957f-03ad4a113431")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

