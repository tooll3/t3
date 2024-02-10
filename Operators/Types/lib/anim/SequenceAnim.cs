using System;
using System.Collections.Generic;
using System.Text;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

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
        
        // Only for custom UI
        public List<float> CurrentSequence = _emptySequence;
        public int CurrentSequenceIndex { get; private set; }
        public float NormalizedBarTime { get; private set; }
        public bool IsRecording { get; private set; }
        
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


        
        private void Update(EvaluationContext context)
        {
            _minValue = MinValue.GetValue(context);
            _maxValue = MaxValue.GetValue(context);
            _bias = Bias.GetValue(context);
            _rate = Rate.GetValue(context);
            
            var outputMode = (OutputModes)OutputMode.GetValue(context).Clamp(0, Enum.GetValues(typeof(OutputModes)).Length - 1);
            
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
            var overrideTime = OverrideTime.GetValue(context) / CurrentSequence.Count;
            if (OverrideTime.IsConnected)
            {
                time = overrideTime;
            }
                 
            NormalizedBarTime = (float)(time % 1).Clamp(0, 0.999999f);

            var updateMode = (UpdateModes)UpdateMode.GetValue(context).Clamp(0, Enum.GetNames(typeof(UpdateModes)).Length - 1);
            switch (updateMode)
            {
                case UpdateModes.Random:
                {
                    var seedValue = (uint)(time *CurrentSequence.Count);
                    var randomValue = MathUtils.XxHash(seedValue);
                    var fraction = ((NormalizedBarTime * CurrentSequence.Count) % 1 / CurrentSequence.Count).Clamp(0, 0.999999f); 
                    NormalizedBarTime = ((float)(randomValue % CurrentSequence.Count)/CurrentSequence.Count + fraction ).Clamp(0, 0.999999f);
                    Result.Value = NormalizedBarTime;
                    break;
                }
                case UpdateModes.PingPong:
                    var modTime = MathUtils.Fmod((time + 1) / 2, 1);
                    NormalizedBarTime = (float)Math.Abs(2 * modTime - 1).Clamp(0, 0.999999f);
                    break;
            }

            var stepIndex = (int)Math.Floor(NormalizedBarTime * CurrentSequence.Count);
            
            if (stepIndex < 0)
                stepIndex = 0;

            WasStep.Value = CurrentSequence.Count > 0 && stepIndex != _lastStepIndex && CurrentSequence[stepIndex] > 0;
            _lastStepIndex = stepIndex;

            var stepStrength = CurrentSequence[stepIndex];

            var stepTime = (float)(NormalizedBarTime * CurrentSequence.Count - stepIndex).Clamp(0, 1);
            var biasedTime = SchlickBias(stepTime, _bias);
            var stepBeat = (1 - biasedTime) * stepStrength;

            float returnValue = 0;
            
            switch (outputMode)
            {
                case OutputModes.Pulse:
                    returnValue = MathUtils.Lerp(_minValue, _maxValue, stepBeat);
                    break;
                
                case OutputModes.NormalizedValue:
                    var normalizedStrength = stepStrength;
                    returnValue = MathUtils.Lerp(_minValue, _maxValue,  SchlickBias( normalizedStrength, _bias) );
                    break;
                
                case OutputModes.CharacterValue:
                    returnValue = stepStrength * MaxCharacterValue;
                    break;
            }
            Result.Value = returnValue;
            
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
                    sequenceValues.Add((v / MaxCharacterValue).Clamp(0, 1));
                }

                _sequences.Add(sequenceValues);
            }

            if (_sequences.Count == 0)
            {
                CurrentSequence = _emptySequence;
            }
            else
            {
                CurrentSequence = _sequences[CurrentSequenceIndex.Mod(_sequences.Count)];
            }
        }
        

        private float SchlickBias(float x, float bias)
        {
            return x / ((1 / bias - 2) * (1 - x) + 1);
        }

        private List<List<float>> _sequences = new(16);
        private static readonly List<float> _emptySequence = new();
        private string _sequencesDefinition = string.Empty;
        private const float MaxCharacterValue = 8;
        
        private double _recordingStartTime = double.NegativeInfinity;
        private int _lastRecordedIndex = 0;

        private int _lastStepIndex;
        private float _lastRecordValue;
        private float _minValue;
        private float _maxValue;
        private float _rate;
        private float _bias;

        private enum OutputModes
        {
            Pulse,
            NormalizedValue,
            CharacterValue,
        }
        
        private enum UpdateModes
        {
            Time,
            PingPong,
            Random,
        }
        

        [Input(Guid = "7BDFB9A8-87B3-4603-890C-FE755F4C4492")]
        public readonly InputSlot<string> Sequence = new();

        [Input(Guid = "F8AFEE64-627D-48E4-B4EA-A15601A01AA1")]
        public readonly InputSlot<int> SequenceIndex = new();

        [Input(Guid = "E3AD4E64-F7CF-49A1-B3AE-4D285C05103A", MappedType = typeof(UpdateModes))]
        public readonly InputSlot<int> UpdateMode = new();
        
        [Input(Guid = "F0AE47AE-5849-4D81-BAE0-9B6EC44949EF")]
        public readonly InputSlot<float> Rate = new();

        [Input(Guid = "5B86B2EC-1043-4A90-9A45-F629CFB7F713", MappedType = typeof(OutputModes))]
        public readonly InputSlot<int> OutputMode = new();

        [Input(Guid = "3AD1C6AB-C91A-4261-AEA5-29394D2926DF")]
        public readonly InputSlot<float> MinValue = new();

        [Input(Guid = "C5FFCF77-14FE-4A59-B41A-26579EF39326")]
        public readonly InputSlot<float> MaxValue = new();

        [Input(Guid = "1c9afc39-7bab-4042-86eb-c7e30595af8e")]
        public readonly InputSlot<float> Bias = new();
        
        [Input(Guid = "3CE54CA0-CD50-4DC2-BD3D-51E26EBF05CE")]
        public readonly InputSlot<float> RecordValue = new();
        
        [Input(Guid = "0E2FB821-460A-4147-99C0-5F6C696E7847")]
        public readonly InputSlot<float> OverrideTime = new();

        
    }
}