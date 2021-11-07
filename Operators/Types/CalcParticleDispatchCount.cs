using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_afc6379d_c940_4617_9e79_0ae129a2f349
{
    public class CalcParticleDispatchCount : Instance<CalcParticleDispatchCount>
    {
        [Output(Guid = "39ac07e0-18a3-4e94-adbf-85cb35acd4f6")]
        public readonly Slot<SharpDX.Int3> DispatchCount = new Slot<SharpDX.Int3>();

        public CalcParticleDispatchCount()
        {
            DispatchCount.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var particleSystem = ParticleSystem.GetValue(context);
            if (particleSystem == null)
                return;
            
            var groupSize = ThreadGroupSize.GetValue(context);
            DispatchCount.Value = (groupSize.X > 0) ? new Int3(particleSystem.MaxCount / groupSize.X, 1, 1) : Int3.Zero;
        }

        [Input(Guid = "2767954c-6836-402a-af76-d5b1c84c20d3")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "7e97ad11-385b-44b3-8db1-d48906bb98cb")]
        public readonly InputSlot<SharpDX.Int3> ThreadGroupSize = new InputSlot<SharpDX.Int3>();
    }
}