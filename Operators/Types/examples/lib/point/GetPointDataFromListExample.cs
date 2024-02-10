using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_25d26231_730e_4376_b256_e34eca6290ce
{
    public class GetPointDataFromListExample : Instance<GetPointDataFromListExample>
    {
        [Output(Guid = "ca72b890-96df-40ee-bb64-240b54edf483")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

