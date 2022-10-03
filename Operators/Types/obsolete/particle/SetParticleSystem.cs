using System.Diagnostics;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5d96bd26_72f6_4285_bd69_688d223fe980
{
    public class SetParticleSystem : Instance<SetParticleSystem>
    {
       
        [Output(Guid = "6DF2BABC-D859-4124-866E-86C3FBCFDD2A", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Result = new Slot<Command>();
        
        public SetParticleSystem()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            //Log.Debug("Setting Particle system");
            var previousSystem = context.ParticleSystem;
            var particleSystemForContext = ParticleSystem.GetValue(context);
            if(particleSystemForContext != null)
                context.ParticleSystem = particleSystemForContext;
            
            // Execute subtree
            Result.Value = SubTree.GetValue(context);
            context.ParticleSystem = previousSystem;
        }
        
        [Input(Guid = "9704B2CF-8795-486C-9810-2BD24E181273")]
        public readonly InputSlot<Command> SubTree = new InputSlot<Command>();
        
        [Input(Guid = "B34677FC-766A-4846-8C4A-843D4E9DC168")]
        public readonly InputSlot<ParticleSystem> ParticleSystem = new InputSlot<ParticleSystem>();
        
    }
}