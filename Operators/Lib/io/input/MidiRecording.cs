using System.Runtime.InteropServices;
using T3.Core.DataTypes.DataSet;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace lib.io.input
{
	[Guid("2d1c9633-b66e-4958-913c-116ae36963a5")]
    public class MidiRecording : Instance<MidiRecording>
    {
        [Output(Guid = "f89b5a87-a757-4f10-aa13-396c2cd9829b")]
        public readonly Slot<DataSet> DataSet = new();
        
        public MidiRecording()
        {
            DataSet.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            if (DataRecording.ActiveRecordingSet == null)
                return;
            
            var wasResetTriggered = MathUtils.WasTriggered(ResetTrigger.GetValue(context), ref _resetTrigger);
            if (wasResetTriggered)
            {
                DataRecording.ActiveRecordingSet.Clear();
            }

            DataSet.Value = DataRecording.ActiveRecordingSet;
        }

        [Input(Guid = "9b844a51-d108-426e-a264-d570d30031c6")]
        public readonly InputSlot<bool> ResetTrigger = new();

        private bool _resetTrigger;
    }
}
