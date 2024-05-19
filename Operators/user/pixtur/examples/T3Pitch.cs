using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_cfdf9331_07e9_46ce_b62c_efec76fc9c9e
{
    public class T3Pitch : Instance<T3Pitch>
    {
        [Output(Guid = "827e06e0-4c7c-40c5-90bb-72cc8ad3fedb")]
        public readonly Slot<Texture2D> Output = new();


    }
}

