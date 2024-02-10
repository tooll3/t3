using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_393fedbb_f442_43ac_bcfa_e618e77591dc
{
    public class CCATestField : Instance<CCATestField>
    {
        [Output(Guid = "8777dbc0-fdfe-407d-badd-f625b5eeeef6")]
        public readonly Slot<Texture2D> Output = new();


    }
}

