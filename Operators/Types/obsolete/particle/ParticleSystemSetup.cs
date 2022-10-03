using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_181c69cd_a251_4891_af67_78aa70af2f90
{
    public class ParticleSystemSetup : Instance<ParticleSystemSetup>
    {
        [Output(Guid = "544D34F9-07BC-4CC1-A657-B9F832DA52E0")]
        public readonly Slot<ParticleSystem> ParticleSystem = new Slot<ParticleSystem>();
        
        public ParticleSystemSetup()
        {
            ParticleSystem.UpdateAction = Update;
            ParticleSystem.Value = new ParticleSystem();
        }

        private void Update(EvaluationContext context)
        {
            if (MaxCount.DirtyFlag.IsDirty)
            {
                int maxCount = MaxCount.GetValue(context);
                ParticleSystem.Value.MaxCount = maxCount;
                ParticleSystem.Value.Init();
            }

            if (MaxEmitRatePerFrame.DirtyFlag.IsDirty)
            {
                MaxEmitRatePerFrame.GetValue(context);
            }
            Log.Info("particle system setup updated");
        }

        [Input(Guid = "62575BBF-1F2B-45CC-9A77-A5F41BFFF09C")]
        public readonly InputSlot<int> MaxCount = new InputSlot<int>(2048);
        
        [Input(Guid = "F8FD66CA-F2D1-421B-878E-A306CB4EC70F")]
        public readonly InputSlot<int> MaxEmitRatePerFrame = new InputSlot<int>(16);
    }
}