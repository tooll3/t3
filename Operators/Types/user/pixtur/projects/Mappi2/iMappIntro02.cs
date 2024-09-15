using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace Types.user.pixtur.projects.Mappi2
{
    [Guid("d19551f2-2941-438c-ab0d-bfc7cef032d9")]
    public class iMappIntro02 : Instance<iMappIntro02>
    {
        [Output(Guid = "e3df1bc8-9b91-46f5-ab16-742529febcf6")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

