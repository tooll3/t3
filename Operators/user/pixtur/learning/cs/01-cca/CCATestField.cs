using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.learning.cs._01_cca
{
	[Guid("393fedbb-f442-43ac-bcfa-e618e77591dc")]
    public class CCATestField : Instance<CCATestField>
    {
        [Output(Guid = "8777dbc0-fdfe-407d-badd-f625b5eeeef6")]
        public readonly Slot<Texture2D> Output = new();


    }
}

