using System;
using System.Collections.Generic;
using T3.Core.Animation;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
// ReSharper disable InconsistentNaming

namespace T3.Operators.Types.Id_f997d943_7a71_4fdf_b182_7a521a73ba27
{
    public class DelayBoolean : Instance<DelayBoolean>
    {
        [Output(Guid = "377beba4-b9bb-4e76-aa81-b37f27b40bd9", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> DelayedTrigger = new();
        
        public DelayBoolean()
        {
            DelayedTrigger.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            if (Math.Abs(context.LocalFxTime - _lastUpdateTime) < 0.001f)
                return;

            _lastUpdateTime = context.LocalFxTime;
            var frameCount = FrameCount.GetValue(context).Clamp(0, 500);
            var current = Trigger.GetValue(context);
            
            var result = false;
            if (frameCount == 0)
            {
                _queue.Clear();
                result = current;
            }
            else
            {
                while (_queue.Count > frameCount)
                {
                    result = _queue.Dequeue();
                }
                
                _queue.Enqueue(current);
            }

            DelayedTrigger.Value = result;

        }

        private double _lastUpdateTime;
        private readonly Queue<bool> _queue = new();

        [Input(Guid = "2141f8a4-0122-48a7-ae78-4f9b79e9ef1f")]
        public readonly InputSlot<bool> Trigger = new();
        
        [Input(Guid = "75DDA16E-7F61-43D6-A41E-AEE8C56CA13E")]
        public readonly InputSlot<int> FrameCount = new();


        
    }
}