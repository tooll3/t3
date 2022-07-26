using System;
using System.Collections.Generic;
using System.Text;
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

        [Output(Guid = "1303BDF7-E547-44BB-84FC-1DE0DF50F1AF", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> WasStep = new();


        public SequenceAnim()
        {
            Result.UpdateAction = Update;
            WasStep.UpdateAction = Update;
        }

        
        public void SetStepValue( int stepIndex, float normalizedValue)
        {
            if (_sequences.Count == 0)
            {
                return;
            }

            var charIndex = 0;
            var sequenceSearchIndex = 0;
            var stepSearchIndex = 0;
            while (charIndex < _sequencesDefinition.Length)
            {
                if (sequenceSearchIndex == CurrentSequenceIndex && stepSearchIndex == stepIndex)
                {
                    var newStringBuilder = new StringBuilder(_sequencesDefinition);
                    newStringBuilder[charIndex] = Convert.ToChar('0' + (int)(normalizedValue*9));
                    _sequencesDefinition = newStringBuilder.ToString();
                    Sequence.Value = _sequencesDefinition;
                    
                    Sequence.TypedInputValue.Value = (_sequencesDefinition);
                    Sequence.DirtyFlag.Invalidate();
                    Sequence.Input.IsDefault = false;
                    UpdateSequences();
                    return;
                }

                var c = _sequencesDefinition[charIndex];
                if (c == '\n')
                {
                    sequenceSearchIndex++;
                    stepSearchIndex = 0;
                }
                else
                {
                    stepSearchIndex++;
                }
                charIndex ++;
            }
        }


        private double _recordingStartTime = double.NegativeInfinity;
        private int _lastRecordedIndex = 0;
        public bool IsRecording { get; private set; }
        
        private void Update(EvaluationContext context)
        {
            _minValue = MinValue.GetValue(context);
            _maxValue = MaxValue.GetValue(context);
            _bias = Bias.GetValue(context);
            _rate = Rate.GetValue(context);

            
            var recValue = RecordValue.GetValue(context);
            if (_recordingStartTime > context.LocalFxTime)
                _recordingStartTime = double.NegativeInfinity;
            
            var timeSinceRecordStart = (context.LocalFxTime - _recordingStartTime);
            IsRecording = MathF.Abs(_rate) > 0.001f && timeSinceRecordStart < (1 / _rate) - (1f/CurrentSequence.Count)/_rate;
            
            var wasRecordTriggered = (Math.Abs(recValue - _lastRecordValue) > 0.0001f && recValue > _lastRecordValue);
            if (wasRecordTriggered)
            {
                _lastRecordedIndex = _lastStepIndex;
                SetStepValue(_lastStepIndex, recValue.Clamp(0, 1));
                if (!IsRecording)
                {
                    _recordingStartTime = context.LocalFxTime;
                }
            }
            else if (IsRecording && _lastStepIndex != _lastRecordedIndex)
            {
                SetStepValue(_lastStepIndex, 0);
                _lastRecordedIndex = _lastStepIndex;
            }

            _lastRecordValue = recValue;
            

            var hasIndexChanged = SequenceIndex.DirtyFlag.IsDirty;
            CurrentSequenceIndex = SequenceIndex.GetValue(context);

            var newSequences = Sequence.GetValue(context);

            if (newSequences != _sequencesDefinition || hasIndexChanged)
            {
                _sequencesDefinition = newSequences;
                UpdateSequences();
            }

            if (CurrentSequence.Count == 0)
            {
                Result.Value = 0;
                return;
            }

            var time = context.LocalFxTime * _rate;
            NormalizedBarTime = (float)(time % 1).Clamp(0, 0.999999f);

            var stepIndex = (int)Math.Floor(NormalizedBarTime * CurrentSequence.Count);
            
            if (stepIndex < 0)
                stepIndex = 0;

            WasStep.Value = CurrentSequence.Count > 0 && stepIndex != _lastStepIndex && CurrentSequence[stepIndex] > 0;
            _lastStepIndex = stepIndex;

            var stepStrength = CurrentSequence[stepIndex];

            var stepTime = (float)(NormalizedBarTime * CurrentSequence.Count - stepIndex).Clamp(0, 1);
            var biasedTime = SchlickBias(stepTime, _bias);
            var stepBeat = (1 - biasedTime) * stepStrength;

            Result.Value = MathUtils.Lerp(_minValue, _maxValue, stepBeat);
            
            Result.DirtyFlag.Clear();
            WasStep.DirtyFlag.Clear();
        }

        private void UpdateSequences()
        {
            var sequences = _sequencesDefinition.Split("\n");
            _sequences.Clear();

            foreach (var s in sequences)
            {
                var minChar = (int)'0';
                var sequenceValues = new List<float>(s.Length);

                foreach (var c in s)
                {
                    var v = c - minChar;
                    sequenceValues.Add((v / MaxAttachValue).Clamp(0, 1));
                }

                _sequences.Add(sequenceValues);
            }

            if (_sequences.Count == 0)
            {
                CurrentSequence = _emptySequence;
            }
            else
            {
                var index = CurrentSequenceIndex.Clamp(0, _sequences.Count - 1);
                CurrentSequence = _sequences[index];
            }
        }
        
        public List<float> CurrentSequence = _emptySequence;
        public int CurrentSequenceIndex { get; private set; }
        public float NormalizedBarTime { get; private set; }

        private float SchlickBias(float x, float bias)
        {
            return x / ((1 / bias - 2) * (1 - x) + 1);
        }

        private List<List<float>> _sequences = new(16);
        private static readonly List<float> _emptySequence = new();
        private string _sequencesDefinition = string.Empty;
        private const float MaxAttachValue = 9;

        private int _lastStepIndex;
        private float _lastRecordValue;
        private float _minValue;
        private float _maxValue;
        private float _rate;
        private float _bias;
        

        [Input(Guid = "7BDFB9A8-87B3-4603-890C-FE755F4C4492")]
        public readonly InputSlot<string> Sequence = new();

        [Input(Guid = "F8AFEE64-627D-48E4-B4EA-A15601A01AA1")]
        public readonly InputSlot<int> SequenceIndex = new();

        [Input(Guid = "F0AE47AE-5849-4D81-BAE0-9B6EC44949EF")]
        public readonly InputSlot<float> Rate = new();

        [Input(Guid = "3AD1C6AB-C91A-4261-AEA5-29394D2926DF")]
        public readonly InputSlot<float> MinValue = new();

        [Input(Guid = "C5FFCF77-14FE-4A59-B41A-26579EF39326")]
        public readonly InputSlot<float> MaxValue = new();

        [Input(Guid = "1c9afc39-7bab-4042-86eb-c7e30595af8e")]
        public readonly InputSlot<float> Bias = new();
        
        [Input(Guid = "3CE54CA0-CD50-4DC2-BD3D-51E26EBF05CE")]
        public readonly InputSlot<float> RecordValue = new();
    }
}