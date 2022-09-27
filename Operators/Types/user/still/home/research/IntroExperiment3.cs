using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_fd123bb8_1182_46ba_97e8_2d4135e17014
{
    public class IntroExperiment3 : Instance<IntroExperiment3>
    {

        [Output(Guid = "f4240bde-9d29-4057-97d0-e23a7f9076fd")]
        public readonly TimeClipSlot<T3.Core.Command> CommandClip = new TimeClipSlot<T3.Core.Command>();


    }
}

