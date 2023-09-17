using SharpDX.Direct3D11;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e62c1fa0_6fcd_49f5_9cf8_d3081c8a5917
{
    public class GetParticleComponents : Instance<GetParticleComponents>, IStatusProvider
    {
        [Output(Guid = "32280e1a-792b-481e-8a5d-5070f684aab8", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<UnorderedAccessView> PointsUav = new();

        [Output(Guid = "231FEEFD-B07D-4FCD-9BD1-B74D0CD765B5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<UnorderedAccessView> SimPointsUav = new();

        [Output(Guid = "2814600a-c45e-4bf8-ab24-b9d3c40d8077", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> Length = new();

        public GetParticleComponents()
        {
            PointsUav.UpdateAction = Update;
            SimPointsUav.UpdateAction = Update;
            Length.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (context == null || context.ParticleSystem == null)
            {
                _lastErrorMessage = "Particle system not set. Is ParticleSimulation not attached?";
                return;
            }
            
            if (context.ParticleSystem?.PointBuffer?.Uav == null
                || context.ParticleSystem?.PointSimBuffer?.Uav == null)
            {
                _lastErrorMessage = "Particle system buffers not valid.";
                return;
            }
            
            _lastErrorMessage = null;
            
            PointsUav.Value = context.ParticleSystem.PointBuffer.Uav;
            SimPointsUav.Value = context.ParticleSystem.PointSimBuffer.Uav;
            
            Length.Value = context.ParticleSystem.PointBuffer.Srv.Description.Buffer.ElementCount;
            
            PointsUav.DirtyFlag.Clear();
            SimPointsUav.DirtyFlag.Clear();
            Length.DirtyFlag.Clear();
        }
        
        // [Input(Guid = "0c642f03-a9e8-403d-9cc5-1d8294454b01")]
        // public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> BufferWithViews = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_lastErrorMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Error;
        }

        public string GetStatusMessage()
        {
            return _lastErrorMessage;
        }

        public string _lastErrorMessage;
    }
}