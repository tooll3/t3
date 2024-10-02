using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ddf3077b_6273_4023_88e5_2948312e012b
{
    public class ReactionDiffusionExample : Instance<ReactionDiffusionExample>
    {
        [Output(Guid = "7f8c561d-9683-4504-9e25-61064f7f6345")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

