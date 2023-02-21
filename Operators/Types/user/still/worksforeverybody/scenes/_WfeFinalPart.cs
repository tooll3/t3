using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1026538b_f021_43f0_b6bd_dda89c57de94
{
    public class _WfeFinalPart : Instance<_WfeFinalPart>
    {
        [Output(Guid = "6b39795e-fd39-4aa8-8cd0-190dac5356d5")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Output(Guid = "4d077ee1-277d-474d-889e-30db32471ce8")]
        public readonly Slot<T3.Core.DataTypes.Command> CommandOutput = new Slot<T3.Core.DataTypes.Command>();


    }
}

