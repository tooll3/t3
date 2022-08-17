using System;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0bc39951_6eec_493c_b609_9e07a9a1dcee
{
    public class GenArMarker : Instance<GenArMarker>
    {
        [Output(Guid = "457c5d64-9937-46f1-bfde-df3db22240a5")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();


        [Input(Guid = "c4ee2560-49c8-403d-9d91-331017782544")]
        public readonly InputSlot<int> A = new InputSlot<int>();

    }
}

