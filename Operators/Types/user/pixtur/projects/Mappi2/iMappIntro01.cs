using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.projects.Mappi2
{
    [Guid("d3a7c4ff-6f3b-4d50-a542-6579cb11fab3")]
    public class iMappIntro01 : Instance<iMappIntro01>
    {
        [Output(Guid = "0a73c25c-88d9-4249-85b0-d7678974fa41")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

