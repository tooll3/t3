using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user.wake.livecoding.helpers
{
    [Guid("58060f7a-2890-4cbe-8ca9-dab4ab15dccd")]
    public class _SwitchSyncTriggers : Instance<_SwitchSyncTriggers>
    {
        [Output(Guid = "8b09e16e-899b-4933-b88a-95e506a533f3")]
        public readonly Slot<bool> Selected = new();

        public _SwitchSyncTriggers()
        {
            Selected.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var varName = IsSimulationVariable.GetValue(context);
            
            if (context.FloatVariables.TryGetValue(varName, out var isSimulation) && isSimulation > 0.5f)
            {
                Selected.Value = SimulatedTrigger.GetValue(context);
            }
            else
            {
                Selected.Value = LiveTrigger.GetValue(context);
            }
        }
        
        [Input(Guid = "1e25e90a-b537-438b-9e39-9b70967716e8")]
        public readonly InputSlot<bool> LiveTrigger = new();

        [Input(Guid = "5236EE24-44DB-462C-B9FA-C3C842CEB0FA")]
        public readonly InputSlot<bool> SimulatedTrigger = new();
        
        [Input(Guid = "2CFF056F-1886-46A1-9E8C-998D5BDEC279")]
        public readonly InputSlot<string> IsSimulationVariable = new();

        
    }
}