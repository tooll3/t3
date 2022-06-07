using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_2443b2fd_c397_4ea6_9588_b595f918cf01
{
    public class DidTimeChange : Instance<DidTimeChange>
    {
        [Output(Guid = "4883b1ec-16c1-422f-8db6-c74c3d48e5be", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> HasChanged = new();
        

        public DidTimeChange()
        {
            HasChanged.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var threshold = Threshold.GetValue(context);
            var mode = (Modes)Mode.GetValue(context);
            var whichTime = (Times)WhichTime.GetValue(context);

            double time = 0;

            switch (whichTime)
            {
                
                case Times.LocalTime:
                    time = context.LocalTime;
                    break;
                
                case Times.GlobalTime:
                    time = context.Playback.TimeInBars;
                    break;
                
                case Times.GlobalFxTime:
                    time = context.Playback.FxTimeInBars;
                    break;
                
                case Times.LocalFxTime:
                default:
                    time = context.LocalFxTime;
                    break;
            }
            
            
            var wasRewind = time < _lastTime - threshold;
            var wasAdvanced = time > _lastTime + threshold;
            
            _lastTime = time;

            bool hasChanged = false;
            
            switch (mode)
            {
                case Modes.DidRewind:
                    hasChanged = wasRewind;
                    break;
                
                case Modes.DidAdvanced:
                    hasChanged = wasAdvanced;
                    break;
                
                case Modes.DidChange:
                    hasChanged = wasAdvanced | wasRewind;
                    break;
            }
            
            
            // if (wasRewind)
            // {
            //     _triggeredLastFrame = true;
            //     HasChanged.Value = true;
            //     return;
            // }
            // else
            // {
            //     if (_triggeredLastFrame)
            //     {
            //         _triggeredLastFrame = false;
            //         HasChanged.Value = false;
            //         return;
            //     }
            // }

            HasChanged.Value = hasChanged;
            
            HasChanged.DirtyFlag.Clear(); // FIXME: is this necessary?
        }

        private enum Modes
        {
            DidRewind,
            DidAdvanced,
            DidChange,
        }

        private enum Times
        {
            LocalTime,
            LocalFxTime,
            GlobalTime,
            GlobalFxTime,
        }
        
        private double _lastTime;
        //private bool _triggeredLastFrame; 
        
        [Input(Guid = "AA73CDBA-F295-446D-9693-53055CA4EDC6")]
        public readonly InputSlot<float> Threshold = new();

        [Input(Guid = "BC112889-77A8-4967-A9B7-683B7C7017FE", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();

        [Input(Guid = "5AC02169-6B75-4176-AE0A-04CAB6C4CE13", MappedType = typeof(Times))]
        public readonly InputSlot<int> WhichTime = new();

        
    }
}