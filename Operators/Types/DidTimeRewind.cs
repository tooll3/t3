using System.Diagnostics;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2443b2fd_c397_4ea6_9588_b595f918cf01
{
    public class DidTimeRewind : Instance<DidTimeRewind>
    {
        [Output(Guid = "4883b1ec-16c1-422f-8db6-c74c3d48e5be", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> HasChanged = new Slot<bool>();
        

        public DidTimeRewind()
        {
            HasChanged.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var threshold = Threshold.GetValue(context);

            var wasRewind = context.TimeForKeyframes < _lastTime - threshold;
            _lastTime = context.TimeForKeyframes;
            if (wasRewind)
            {
                _triggeredLastFrame = true;
                HasChanged.Value = true;
                return;
            }
            else
            {
                if (_triggeredLastFrame)
                {
                    _triggeredLastFrame = false;
                    HasChanged.Value = false;
                    return;
                }
            }

            HasChanged.Value = false;
            HasChanged.DirtyFlag.Clear();
        }


        private double _lastTime;
        private bool _triggeredLastFrame; 
        
        [Input(Guid = "AA73CDBA-F295-446D-9693-53055CA4EDC6")]
        public readonly InputSlot<float> Threshold = new InputSlot<float>();
        
    }
}