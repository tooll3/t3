using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f7504864_ddbe_4af5_91ff_f4cfa36f9ec1
{
    public class SoundLevelExample : Instance<SoundLevelExample>
    {
        [Output(Guid = "8a36e0fd-a200-4195-8235-e092549d9df5")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

