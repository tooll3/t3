using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3ce7996f_dd8a_4deb_9cd5_d0aed584026f
{
    public class FragmentNoiseExample : Instance<FragmentNoiseExample>
    {
        [Output(Guid = "155b810c-19f7-4f0f-a4b4-ab0b18d2743a")]
        public readonly Slot<Texture2D> Output = new();


    }
}

