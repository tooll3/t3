using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_70e97e6b_3ddf_4a88_b080_c63fdbd251c9
{
    public class UVsViewerExample : Instance<UVsViewerExample>
    {
        [Output(Guid = "7c291a16-1e48-40c6-9fbc-cab14bb80720")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

