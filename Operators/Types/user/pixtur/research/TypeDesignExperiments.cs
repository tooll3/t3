using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_24dc052c_562e_4b4e_a59b_ab9bf55ba01d
{
    public class TypeDesignExperiments : Instance<TypeDesignExperiments>
    {
        [Output(Guid = "93d5e7b7-2e2d-468d-80e6-322ba4134190")]
        public readonly Slot<Texture2D> Output = new();


    }
}

