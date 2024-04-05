using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_8fc90e3e_169b_41ba_a76a_51e74f183eb4
{
    public class ProceduralMaterialExample : Instance<ProceduralMaterialExample>
    {
        [Output(Guid = "00a05165-eb5e-4643-812c-3363fa5bd0f2")]
        public readonly Slot<Texture2D> ImgOutput = new Slot<Texture2D>();


    }
}

