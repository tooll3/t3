using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.project.ccctable
{
	[Guid("8be91ab1-a61d-4352-baf6-e74838fb657c")]
    public class CCCampTable : Instance<CCCampTable>
    {
        [Output(Guid = "edf83ded-1331-41fa-a078-4ff141ade488")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

