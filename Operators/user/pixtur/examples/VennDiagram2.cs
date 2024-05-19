using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1a8acd4a_e6a9_45ad_b5be_d564e0fd3f18
{
    public class VennDiagram2 : Instance<VennDiagram2>
    {
        [Output(Guid = "9834a3a3-fba0-475e-9f0f-4014f90fccf6")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

