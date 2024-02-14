using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9e52c10e_d17e_4b5a_aaec_d8ffa78a2426
{
    public class CynicTestLab : Instance<CynicTestLab>
    {
        [Output(Guid = "56e30a16-36dd-4dba-b6ba-57736af71acd")]
        public readonly Slot<Texture2D> Output = new();


    }
}

