using System.Collections.Generic;
using System.Linq;
using Core.Audio;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b72d968b_0045_408d_a2f9_5c739c692a66
{
    public class SoundInput : Instance<SoundInput>
    {
        [Output(Guid = "B3EFDF25-4692-456D-AA48-563CFB0B9DEB", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> FftBuffer = new();

        [Output(Guid = "b438986f-6ef9-40d5-8a2c-b00c01578ebc", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        [Output(Guid = "D7D2A87C-4231-4F8B-904F-6E5F5D01B1D8", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> AvailableData = new();

        public SoundInput()
        {
            Result.UpdateAction = Update;
            FftBuffer.UpdateAction = Update;
            AvailableData.UpdateAction = Update;
        }


        private static List<float> _emptyList = new List<float>();
        
        private void Update(EvaluationContext context)
        {
            if(!WasapiAudioInput.DevicesInitialized)
                WasapiAudioInput.Initialize();

            FftBuffer.Value = AudioInput.FrequencyBandPeaks == null 
                                  ? _emptyList 
                                  : AudioInput.FrequencyBandPeaks.ToList(); 
            
        }
        
        protected override void Dispose(bool disposing)
        {
            //_analyzer.Dispose();
        }

        private bool _wasReset;

        
        [Input(Guid = "E278C4C8-4C3A-4F95-BEEC-CE8EAD827724")]
        public readonly InputSlot<bool> Reset = new();
        
        [Input(Guid = "BCD6C387-7FEC-494F-B956-560728F05BFF")]
        public readonly InputSlot<float> Gain = new();
    }
}