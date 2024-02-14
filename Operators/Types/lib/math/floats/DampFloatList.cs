using CppSharp.Types.Std;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;
using T3.Operators.Types.Id_cc07b314_4582_4c2c_84b8_bb32f59fc09b;

namespace T3.Operators.Types.Id_c30ba288_9e40_4636_beb5_68401d91fe37
{
    public class DampFloatList : Instance<DampFloatList>
    {
        [Output(Guid = "23c867c8-d175-463f-bcaa-18e6be5f20c2", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> Result = new();

        private const float MinTimeElapsedBeforeEvaluation = 1 / 1000f;

        public DampFloatList()
        {
            Result.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var inputList = Values.GetValue(context);
            var damping = Damping.GetValue(context);

            var currentTime = UseAppRunTime.GetValue(context) ? Playback.RunTimeInSecs : context.LocalFxTime; 
            if (Math.Abs(currentTime - _lastEvalTime) < MinTimeElapsedBeforeEvaluation)
                return;

            // Clean up result list
            if (inputList == null || inputList.Count == 0)
            {
                return;
            }
            if (Result.Value == null || Result.Value.Count != inputList.Count)
            {
                Result.Value = new List<float>();
            }
            Result.Value.Clear();


            //clean up internal lists
            matchListLength(ref _dampedValues, inputList.Count);
            matchListLength(ref _velocities, inputList.Count);

            void matchListLength(ref List<float> list, int length)
            {
                while(list.Count < length)
                {
                    list.Add(0);
                }
                while(list.Count > length)
                {
                    list.RemoveAt(list.Count - 1);
                }
            }

            // The below is present in Damp.cs, but I'm unsure what it does. Commenting it out doesn't seem to break anything, but will leave it here just in case

            //if (context.IntVariables.TryGetValue("__MotionBlurPass", out var motionBlurPass))
            //{
            //    if (motionBlurPass > 0)
            //    {
            //        //Log.Debug($"Skip motion blur pass {motionBlurPass}");
            //        return;
            //    }                
            //} 

            _lastEvalTime = currentTime;

            var method = (DampFunctions.Methods)Method.GetValue(context).Clamp(0, 1);

            for(var i = 0; i < inputList.Count; i++)
            {
                var velocity = _velocities[i];
                var value = inputList[i];
                var dampedValue = DampFunctions.DampenFloat(value, _dampedValues[i], damping, ref velocity, method);

                MathUtils.ApplyDefaultIfInvalid(ref dampedValue, 0);
                MathUtils.ApplyDefaultIfInvalid(ref velocity, 0);

                _dampedValues[i] = dampedValue;
                _velocities[i] = velocity;
            }
            Result.Value.AddRange(_dampedValues);
        }

        private List<float> _dampedValues = new List<float>(1);
        private List<float> _velocities = new List<float>(1);
        //private float _velocity; //AHHH each item needs its own velocity parameter! they're tangling each other up!
        private double _lastEvalTime;
        
        [Input(Guid = "491cc9cd-28fc-4ec4-8d98-5a7e0d17082a")]
        public readonly InputSlot<List<float>> Values = new(new List<float>(2));

        [Input(Guid = "3d63df3d-81c0-4e27-ae0c-acd3092d952c")]
        public readonly InputSlot<float> Damping = new();
        
        [Input(Guid = "ad97ae08-9aeb-4ed5-b5b1-e24d9af21cf7", MappedType = typeof(DampFunctions.Methods))]
        public readonly InputSlot<int> Method = new();
        
        [Input(Guid = "51bbb9f9-2895-4965-9f73-34d7dbae2ad1")]
        public readonly InputSlot<bool> UseAppRunTime = new();

    }
}
