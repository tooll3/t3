using System.Runtime.InteropServices;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.@string
{
	[Guid("51943340-70b1-4bb5-8cb9-0e79d366a57b")]
    public class JoinStringList : Instance<JoinStringList>
    {
        [Output(Guid = "ef105688-3e28-47c3-8b8e-5fda3bde3090")]
        public readonly Slot<string> Result = new();

        public JoinStringList()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var separator = Separator.GetValue(context).Replace("\\n", "\n");
            var input = Input.GetValue(context);
            if (input == null || input.Count == 0)
            {
                Log.Warning("Can't join empty string list.", this);
                Result.Value = null;
                return;
            }

            Result.Value = string.Join(separator, input);
        }

        [Input(Guid = "DB366216-B485-48BF-B267-56344B317CF7")]
        public readonly InputSlot<List<string>> Input = new ();
        
        [Input(Guid = "89350e3c-2b83-4720-bfa1-d4adc6cc02fa")]
        public readonly InputSlot<string> Separator = new();

    }
}