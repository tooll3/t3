using System;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Operators.Types.Id_ecbafbeb_c14b_4507_953f_80bc6676d077;

namespace T3.Operators.Types.Id_f8aed421_5e0e_4d1f_993c_1801153ebba8
{
    public class AudioReaction : Instance<AudioReaction>
    {
        [Output(Guid = "2aa4d0cb-c49d-41ce-aa74-794cc8682590", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Level = new Slot<float>();

        [Output(Guid = "E1D7D3AF-FFD7-460F-B861-F0A11EE287B0", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> PeakCount = new Slot<int>();

        [Output(Guid = "0CD3D908-C7C4-41D3-BEF2-C48A29B9842A", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> PeakDetected = new Slot<bool>();

        public AudioReaction()
        {
            Level.UpdateAction = Update;
            PeakCount.UpdateAction = Update;
            PeakDetected.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var band = (FrequencyBands)Band.GetValue(context);
            var mode = (Modes)Mode.GetValue(context);
            //var a = _SetAudioAnalysis.AudioAnalysisResult;

            var results = band == FrequencyBands.Bass
                              ? _SetAudioAnalysis.AudioAnalysisResult.Bass
                              : _SetAudioAnalysis.AudioAnalysisResult.HiHats;

            var peakDetected = results.PeakCount > _lastPeakCount;
            
            var decay = Decay.GetValue(context);
            var modulo = UseModulo.GetValue(context);
            var usingModulo = modulo > 0;
            var isModuloPeak = peakDetected && (!usingModulo || results.PeakCount % modulo == 0);

            if (peakDetected && isModuloPeak)
            {
                _lastModuloPeakTime = EvaluationContext.GlobalTimeForEffects;
            }
            var timeSinceModuloPeak = (float)(EvaluationContext.GlobalTimeForEffects - _lastModuloPeakTime);
            var timeSincePeak = usingModulo
                                    ? timeSinceModuloPeak
                                    : results.TimeSincePeak;
            
            float value = 0;
            switch (mode)
            {
                case Modes.TimeSincePeak:
                    
                    value = timeSincePeak;
                    break;

                case Modes.Count:
                    value = usingModulo
                        ? results.PeakCount / modulo
                        : results.PeakCount;
                    break;

                case Modes.Peaks:
                    value = (float)Math.Max(0, 1 - timeSincePeak * decay);
                    break;

                case Modes.PeaksDecaying:
                    value = (float)Math.Pow(decay + 1, -timeSincePeak);
                    break;

                case Modes.Level:
                    value = (float)results.AccumulatedEnergy;
                    break;

                case Modes.MovingSum:
                    _movingSum += Math.Pow(results.AccumulatedEnergy, decay);
                    value = (float)(_movingSum % 10000);
                    break;

                case Modes.RandomValue:
                    if (isModuloPeak)
                    {
                        _lastRandomValue = (float)_random.NextDouble();
                    }
                    value = _lastRandomValue;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            Level.Value = value * Amplitude.GetValue(context);

            PeakCount.Value = results.PeakCount;

            PeakDetected.Value = isModuloPeak;
            _lastPeakCount = results.PeakCount;

            Level.DirtyFlag.Clear();
            PeakCount.DirtyFlag.Clear();
            PeakDetected.DirtyFlag.Clear();
        }

        private double _lastModuloPeakTime;
        private float _lastRandomValue;

        private enum FrequencyBands
        {
            Bass,
            Highs,
        }

        private enum Modes
        {
            TimeSincePeak,
            Peaks,
            PeaksDecaying,
            Level,
            Count,
            RandomValue,
            MovingSum,
        }

        private int _lastPeakCount;
        private double _movingSum = 0;
        private Random _random = new Random();

        [Input(Guid = "15F841F5-5153-4383-90B9-F6A4F72D5D6B", MappedType = typeof(FrequencyBands))]
        public readonly InputSlot<int> Band = new InputSlot<int>();

        [Input(Guid = "D9069774-188B-4A5E-976A-053D0C893503", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new InputSlot<int>();

        [Input(Guid = "EC0FE09B-B925-4B14-8186-8C32B65AF9BB")]
        public readonly InputSlot<float> Amplitude = new InputSlot<float>();

        [Input(Guid = "E7FAC507-AD85-48F4-89D3-76600FF519EC")]
        public readonly InputSlot<float> Decay = new InputSlot<float>();

        [Input(Guid = "0E04D1F8-3BBD-46B1-BDE6-46BCDFDBE3AA")]
        public readonly InputSlot<int> UseModulo = new InputSlot<int>();
    }
}