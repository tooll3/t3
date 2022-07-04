using System.Collections.Generic;
using System.Linq;
using Core.Audio;
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
            if (!WasapiAudioInput.DevicesInitialized)
                WasapiAudioInput.Initialize();

            FftBuffer.Value = AudioInput.FrequencyBandPeaks == null
                                  ? _emptyList
                                  : AudioInput.FrequencyBandPeaks.ToList();
        }

        private static readonly List<float> _emptyList = new();
    }
}