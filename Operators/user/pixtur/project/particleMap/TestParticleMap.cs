using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3b902c86_e284_4a2b_969c_65e79a14ceba
{
    public class TestParticleMap : Instance<TestParticleMap>
    {
        [Output(Guid = "b4234870-9d72-4fef-8345-0e80cbf00801")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

