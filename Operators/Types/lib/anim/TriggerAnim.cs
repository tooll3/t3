using System;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_95d586a2_ee14_4ff5_a5bb_40c497efde95
{
    public class TriggerAnim : Instance<TriggerAnim>
    {
        [Output(Guid = "aac4ecbf-436a-4414-94c1-53d517a8e587", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        public TriggerAnim()
        {
            Result.UpdateAction = Update;
        }

        private enum Directions
        {
            Forward,
            Backwards,
            None
        }

        private void Update(EvaluationContext context)
        {
            _startValue = StartValue.GetValue(context);
            _endValue = EndValue.GetValue(context);
            _bias = Bias.GetValue(context);
            _shape = (Shapes)(int)Shape.GetValue(context).Clamp(0,Enum.GetNames(typeof(Shapes)).Length -1);
            _duration = Duration.GetValue(context);
            _delay = Delay.GetValue(context);

            var animMode = AnimMode.GetEnumValue<AnimModes>(context);//   (AnimModes)AnimMode.GetValue(context).Clamp(0, Enum.GetNames(typeof(AnimModes)).Length -1);
            var triggered = Trigger.GetValue(context);
            if (triggered != _trigger)
            {
                //Log.Debug(" Trigger changed to " + triggered, this);
                _trigger = triggered;
                
                if (animMode == AnimModes.ForwardAndBackwards)
                {
                    _triggerTime = context.Playback.FxTimeInBars;
                    _currentDirection = triggered ? Directions.Forward : Directions.Backwards;
                    _startProgress = LastFraction;
                }
                else
                {
                    if (triggered)
                    {
                        if (animMode == AnimModes.OnlyOnTrue)
                        {
                            _triggerTime = context.Playback.FxTimeInBars;
                            _currentDirection = Directions.Forward;
                            LastFraction = -_delay;
                        }
                    }
                    else
                    {
                        if (animMode == AnimModes.OnlyOnFalse)
                        {
                            _triggerTime = context.Playback.FxTimeInBars;
                            _currentDirection = Directions.Backwards;
                            LastFraction = 1;
                        }
                    }
                }
            }


            if (animMode == AnimModes.ForwardAndBackwards)
            {
                var dp = (float)((context.LocalFxTime - _triggerTime) / _duration);
                if (_currentDirection == Directions.Forward)
                {
                    LastFraction = _startProgress + dp;
                    if (LastFraction >= 1)
                    {
                        LastFraction = 1;
                        _currentDirection = Directions.None;
                    }
                }
                else if (_currentDirection == Directions.Backwards)
                {
                    LastFraction = _startProgress - dp;
                    if (LastFraction <= 0)
                    {
                        LastFraction = 0;
                        _currentDirection = Directions.None;
                    }
                }
            }
            else
            {
                if (_currentDirection == Directions.Forward)
                {
                    LastFraction = (context.LocalFxTime - _triggerTime)/_duration;
                    if(LastFraction >= 1)
                    {
                        LastFraction = 1;
                        _currentDirection = Directions.None;
                    }
                }
                else if  (_currentDirection == Directions.Backwards)
                {
                    LastFraction =   ( _triggerTime- context.LocalFxTime )/_duration;

                    if (LastFraction <= 0)
                    {
                        LastFraction = 0;
                        _currentDirection = Directions.None;
                    }
                }
            }
            
            var normalizedValue = CalcNormalizedValueForFraction(LastFraction);
            if (double.IsNaN(LastFraction) || double.IsInfinity(LastFraction))
            {
                LastFraction = 0;
            }
            
            Result.Value = MathUtils.Lerp(_startValue, _endValue,  normalizedValue);
        }
        
        public float CalcNormalizedValueForFraction(double t)
        {
            //var fraction = CalcFraction(t);
            var value = MapShapes[(int)_shape]((float)t);
            var biased = SchlickBias(value, _bias);
            return biased;
        }

        private float SchlickBias(float x, float bias)
        {
            return x / ((1 / bias - 2) * (1 - x) + 1);
        }


        private delegate float MappingFunction(float fraction);
        private static readonly MappingFunction[] MapShapes =
            {
                f => f.Clamp(0,1), // 0: Linear
                f => MathUtils.SmootherStep(0,1,f.Clamp(0,1)), //1: Smooth Step
                f => MathUtils.SmootherStep(0,1,f.Clamp(0,1)/2) *2   , //2: Easy In
                f => MathUtils.SmootherStep(0,1,f.Clamp(0,1)/2 + 0.5f) *2 -1, //3: Easy Out
                f => MathF.Sin(f.Clamp(0,1) * 40) * MathF.Pow(1-f.Clamp(0.0001f, 1),4) ,  //4: Shake
                f => f<=0 ? 0 : (1-f.Clamp(0,1)), // 5: Kick
            };
        
        private bool _trigger;
        private Shapes _shape;
        private float _bias;
        private float _startValue;
        private float _endValue;
        private float _duration = 1;
        private float _delay;

        private double _startProgress;
        public double LastFraction;
        
        public enum Shapes
        {
            Linear = 0,
            SmoothStep = 1,
            EaseIn = 2,
            EaseOut = 3,
            Shake = 4,
            Kick = 5,
        }


        public enum AnimModes
        {
            OnlyOnTrue,
            OnlyOnFalse,
            ForwardAndBackwards,
        }
        
        private Directions _currentDirection = Directions.None;
        private double _triggerTime;

        [Input(Guid = "62949257-ADB3-4C67-AC0A-D37EE28DA81B")]
        public readonly InputSlot<bool> Trigger = new();

        [Input(Guid = "c0fa79d5-2c49-4d40-998f-4eb0101ae050", MappedType = typeof(Shapes))]
        public readonly InputSlot<int> Shape = new();
        
        [Input(Guid = "9913FCEA-3994-40D6-84A1-D56525B31C43", MappedType = typeof(AnimModes))]
        public readonly InputSlot<int> AnimMode = new();

        [Input(Guid = "0d56fc27-fa15-4f1e-aa09-f97af93d42c7")]
        public readonly InputSlot<float> Duration = new();
        
        [Input(Guid = "3AD8E756-7720-4F43-85DA-EFE1AF364CFE")]
        public readonly InputSlot<float> StartValue = new();
        
        [Input(Guid = "287fa06c-3e18-43f2-a4e1-0780c946dd84")]
        public readonly InputSlot<float> EndValue = new();

        [Input(Guid = "214e244a-9e95-4292-81f5-cd0199f05c66")]
        public readonly InputSlot<float> Delay = new();

        [Input(Guid = "9bfd5ae3-9ca6-4f7b-b24b-f554ad4d0255")]
        public readonly InputSlot<float> Bias = new();
    }
}