using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_04b8f508_2101_42dc_8d91_60b585bc561e
{
    public class SpringExample : Instance<SpringExample>
    {
        [Output(Guid = "4381982d-9e95-44d1-a4cb-d6be0cde4ccb")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

