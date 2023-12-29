using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_179093f6_d3ef_43bc_a5af_1df2379ec081
{
    public class SwitchBlendState : Instance<SwitchBlendState>
    {
        [Output(Guid = "0608B46B-4778-4F95-B688-3A749F9664AE")]
        public readonly Slot<BlendState> Output = new();

        public SwitchBlendState()
        {
            Output.UpdateAction = Update;
        }

        private BlendState[] _blendStates = new BlendState[0];
        private void Update(EvaluationContext context)
        {
            // var blendStates = BlendStates.GetCollectedTypedInputs();
            BlendStates.GetValues(ref _blendStates, context);
            var index = Index.GetValue(context);

            if (_blendStates.Length == 0 || index == -1)
            {
                return;
            }
            
            Output.Value = _blendStates[index.Mod(_blendStates.Length)];
        }

        [Input(Guid = "A737BB60-D98B-4405-914C-7DF91A58D8BC")]
        public readonly MultiInputSlot<BlendState> BlendStates = new();
        
        [Input(Guid = "232a10e8-0357-4adc-935b-9cb1b7938730")]
        public readonly InputSlot<int> Index = new();
    }
}