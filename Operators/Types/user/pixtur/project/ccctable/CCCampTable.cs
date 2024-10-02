using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_8be91ab1_a61d_4352_baf6_e74838fb657c
{
    public class CCCampTable : Instance<CCCampTable>
    {
        [Output(Guid = "edf83ded-1331-41fa-a078-4ff141ade488")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

