using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_99f1799b_089b_43e2_8655_7c6037647c3d
{
    public class TransformImage2Example : Instance<TransformImage2Example>
    {
        [Output(Guid = "a3048d14-fa38-4a5b-9294-369cd306216b")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


    }
}

