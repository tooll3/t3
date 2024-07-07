using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.research.hitform
{
    [Guid("6f3d09b4-5b3e-44cf-b023-7db157682897")]
    public class LoadHFCSExample : Instance<LoadHFCSExample>
    {
        [Output(Guid = "8492ef4f-d86f-466c-873e-9ebbd4490cc3")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

