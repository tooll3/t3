using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_3096f86e_e850_4f76_80de_1996fc811285
{
    public class LoadObjAsPointsExample : Instance<LoadObjAsPointsExample>
    {
        [Output(Guid = "ee4e55ae-e740-430c-82e7-4c3bdb98062d")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

