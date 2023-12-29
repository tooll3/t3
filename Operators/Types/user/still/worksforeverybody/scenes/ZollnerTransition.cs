using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_77b8cf1b_2bd7_4bfb_9f22_57d613560186
{
    public class ZollnerTransition : Instance<ZollnerTransition>
    {
        [Output(Guid = "1426e023-78b1-4800-acf0-7bca954671ef")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

