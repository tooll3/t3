using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_18d9721b_e170_4f4d_b630_30445aba5e20
{
    public class FadingFacesRev2021 : Instance<FadingFacesRev2021>
    {

        [Input(Guid = "71eb23a5-dde7-42bd-916a-5278343b64ad")]
        public readonly InputSlot<float> OverrideTime = new InputSlot<float>();

        [Output(Guid = "29a0bc9a-4c33-4777-aa73-b2c7074a89fa")]
        public readonly TimeClipSlot<T3.Core.Command> Output2 = new TimeClipSlot<T3.Core.Command>();


    }
}

