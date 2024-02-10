using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_05f5733d_0aa1_462e_a02f_f8c639f07152
{
    public class D07EditingTest : Instance<D07EditingTest>
    {
        [Output(Guid = "1e330a2e-a9b4-4c6d-8509-20f9bd13d40d")]
        public readonly Slot<Texture2D> TextureOutput = new();


    }
}

