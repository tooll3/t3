using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b4dec97e_e881_4bdd_9db8_1a3627d155dc
{
    public class IntroExperiment2 : Instance<IntroExperiment2>
    {

        [Output(Guid = "605b1a47-72d2-4d0a-ad99-e20dfef5e898")]
        public readonly TimeClipSlot<T3.Core.Command> CommandClip = new TimeClipSlot<T3.Core.Command>();


    }
}

