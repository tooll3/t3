using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_41eeb2df_a5fe_4ca3_b66e_1639cea1fb5a
{
    public class FloorPlanFractal : Instance<FloorPlanFractal>
    {
        [Output(Guid = "4690ba22-136d-4231-bfcd-95c8ad078848")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

