using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_dbe329c8_f16a_4ebb_bfab_2f90ed9646a2
{
    public class ColorFlowExperiment1 : Instance<ColorFlowExperiment1>
    {
        [Output(Guid = "d5d36e56-bbf1-4992-a983-240454e87768")]
        public readonly Slot<Texture2D> Output = new();


    }
}

