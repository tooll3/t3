using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.cynic.research
{
	[Guid("9e52c10e-d17e-4b5a-aaec-d8ffa78a2426")]
    public class CynicTestLab : Instance<CynicTestLab>
    {
        [Output(Guid = "56e30a16-36dd-4dba-b6ba-57736af71acd")]
        public readonly Slot<Texture2D> Output = new();


    }
}

