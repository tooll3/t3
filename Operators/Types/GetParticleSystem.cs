using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5c983354_efc5_42a4_a0e1_ad548c12c177
{
    public class GetParticleSystem : Instance<GetParticleSystem>
    {
        [Output(Guid = "5403D6A2-C93C-4045-A7D0-14653A1C1AD7", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<ParticleSystem> Result = new Slot<ParticleSystem>();

        public GetParticleSystem()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var overridingSystem = Override.GetValue(context);
            Result.Value = overridingSystem ?? context.ParticleSystem;
            // Log.Debug("Getting particle system to " + Result.Value);
        }

        [Input(Guid = "8109F618-2C24-4340-BF01-80FCF7A924DB")]
        public readonly InputSlot<ParticleSystem> Override = new InputSlot<ParticleSystem>();
    }
}