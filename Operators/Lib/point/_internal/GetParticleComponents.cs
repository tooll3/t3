using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace lib.point._internal
{
	[Guid("e62c1fa0-6fcd-49f5-9cf8-d3081c8a5917")]
    public class GetParticleComponents : Instance<GetParticleComponents>, IStatusProvider
    {
        [Output(Guid = "231FEEFD-B07D-4FCD-9BD1-B74D0CD765B5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<UnorderedAccessView> ParticlesUav = new();

        [Output(Guid = "2814600a-c45e-4bf8-ab24-b9d3c40d8077", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> Length = new();

        [Output(Guid = "641ECE29-7845-43E5-85CA-F33912A1989F", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> SpeedFactor = new();
        
        public GetParticleComponents()
        {
            ParticlesUav.UpdateAction = Update;
            Length.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (context == null || context.ParticleSystem == null)
            {
                _lastErrorMessage = "Particle system not set. Is ParticleSimulation not attached?";
                return;
            }
            
            if (context.ParticleSystem?.ParticleBuffer?.Uav == null)
            {
                _lastErrorMessage = "Particle system buffers not valid.";
                return;
            }
            
            _lastErrorMessage = null;
            
            ParticlesUav.Value = context.ParticleSystem.ParticleBuffer.Uav;
            SpeedFactor.Value = context.ParticleSystem.SpeedFactor;
            
            Length.Value = context.ParticleSystem.ParticleBuffer.Srv.Description.Buffer.ElementCount;
            
            ParticlesUav.DirtyFlag.Clear();
            Length.DirtyFlag.Clear();
            SpeedFactor.DirtyFlag.Clear();
        }
        
        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Error;
        }

        public string GetStatusMessage()
        {
            return _lastErrorMessage;
        }

        private string _lastErrorMessage;
    }
}