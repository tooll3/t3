using System;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_94a392e6_3e03_4ccf_a114_e6fafa263b4f
{
    public class SequenceAnim : Instance<SequenceAnim>
    {
        [Output(Guid = "99f24d0d-bda5-44a5-afd1-cb144ba313e6", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        public SequenceAnim()
        {
            Result.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            _minValue = MinValue.GetValue(context);
            _maxValue = MaxValue.GetValue(context);
            _bias = Bias.GetValue(context);
            _rate = Rate.GetValue(context);

            _sequence = Sequence.GetValue(context);
            if(_sequence.Length == 0){
                Log.Warning("Sequence must be at least a characters long" , SymbolChildId);
                return;
            }
            
            
            var time = context.LocalFxTime * _rate;
            var barTime = (time % 1);
            

            var minChar = (int)'0';
            var maxValue = float.NegativeInfinity;
            foreach(var c in _sequence) 
            {
                var v = c - minChar;
                if( v > maxValue)
                    maxValue = (float)v;                
            }
            
            var stepIndex = (int)Math.Floor( barTime * _sequence.Length) ;
            
            if(stepIndex <0 )
                stepIndex = 0;
                
            
            var stepStrength = _sequence[stepIndex] - minChar;
            var strength = stepStrength / maxValue;

            
            var stepTime = (float)(barTime * _sequence.Length - stepIndex).Clamp(0,1);
            var biasedTime = SchlickBias(stepTime, _bias);
            var stepBeat = (1-biasedTime) * strength;
            
            Result.Value =  MathUtils.Lerp(_minValue, _maxValue,  stepBeat);
        }


        
        private float SchlickBias(float x, float bias)
        {
            return x / ((1 / bias - 2) * (1 - x) + 1);
        }

        
        private bool _trigger;
        
        private float _minValue;
        private float _maxValue;
        private float _rate;
        private float _bias;
        private string _sequence;

        
        [Input(Guid = "7BDFB9A8-87B3-4603-890C-FE755F4C4492")]
        public readonly InputSlot<string> Sequence = new();

        [Input(Guid = "F0AE47AE-5849-4D81-BAE0-9B6EC44949EF")]
        public readonly InputSlot<float> Rate = new();
        
        [Input(Guid = "3AD1C6AB-C91A-4261-AEA5-29394D2926DF")]
        public readonly InputSlot<float> MinValue = new();

        [Input(Guid = "C5FFCF77-14FE-4A59-B41A-26579EF39326")]
        public readonly InputSlot<float> MaxValue = new();

        [Input(Guid = "1c9afc39-7bab-4042-86eb-c7e30595af8e")]
        public readonly InputSlot<float> Bias = new();

    }
}