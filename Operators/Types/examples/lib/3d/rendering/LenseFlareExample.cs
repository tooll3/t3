using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_442995fa_3d89_4d6c_b006_77f825f4e3ed
{
    public class LenseFlareExample : Instance<LenseFlareExample>
    {
        [Output(Guid = "d794d1bc-d322-4868-a894-a26ff5ff7805")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

