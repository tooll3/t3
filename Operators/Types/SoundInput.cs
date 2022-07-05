using System;
using System.Collections.Generic;
using System.Linq;
using Core.Audio;
using T3.Core;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b72d968b_0045_408d_a2f9_5c739c692a66
{
    public class SoundInput : Instance<SoundInput>
    {
        [Output(Guid = "B3EFDF25-4692-456D-AA48-563CFB0B9DEB", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> FftBuffer = new();

        public SoundInput()
        {
            FftBuffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            // if (!WasapiAudioInput.DevicesInitialized)
            //     WasapiAudioInput.Initialize();
            //
            if (!string.IsNullOrEmpty(ProjectSettings.Config.AudioInputDeviceName) && !WasapiAudioInput.DevicesInitialized)
            {
                WasapiAudioInput.Initialize();
            }
            
            var mode = (Modes)Mode.GetValue(context).Clamp(0, Enum.GetNames(typeof(Modes)).Length-1);
            switch (mode)
            {
                case Modes.RawFft:
                    FftBuffer.Value = AudioInput.FftGainBuffer == null
                                          ? _emptyList
                                          : AudioInput.FftGainBuffer.ToList();
                    
                    break;

                case Modes.NormalizedFft:
                    FftBuffer.Value = AudioInput.FftNormalizedBuffer == null
                                          ? _emptyList
                                          : AudioInput.FftNormalizedBuffer.ToList();
                    break;
                
                case Modes.FrequencyBands:
                    FftBuffer.Value = AudioInput.FrequencyBands == null
                                          ? _emptyList
                                          : AudioInput.FrequencyBands.ToList();
                    break;

                case Modes.FrequencyBandsPeaks:
                    FftBuffer.Value = AudioInput.FrequencyBandPeaks == null
                                          ? _emptyList
                                          : AudioInput.FrequencyBandPeaks.ToList();

                    break;
                
                case Modes.FrequencyBandsAttacks:
                    FftBuffer.Value = AudioInput.FrequencyBandAttacks == null
                                          ? _emptyList
                                          : AudioInput.FrequencyBandAttacks.ToList();

                    break;
                
            }
            
        }

        private enum Modes
        {
            RawFft,
            NormalizedFft,
            FrequencyBands,
            FrequencyBandsPeaks,
            FrequencyBandsAttacks,
        }
        
        [Input(Guid = "02A09286-19C9-4CEE-9439-260701F6DE58", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();

        
        private static readonly List<float> _emptyList = new();
    }
}