using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7d7c9abb_1742_407e_85c7_ba4f6e87f390
{
    public class RenderTargetExample : Instance<RenderTargetExample>
    {
        [Output(Guid = "d3295b50-3343-456c-bae1-8c1351b4f875")]
        public readonly Slot<Texture2D> Output = new();


    }
}

