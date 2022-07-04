using System;
using ManagedBass;
using T3.Core;
using T3.Core.IO;

namespace Core.Audio
{
    /// <summary>
    /// Provide audio input either from internal soundtrack or from selected Wasapi device
    /// </summary>
    public static class AudioInput
    {
        public enum InputModes
        {
            Soundtrack,
            WasapiDevice,
        }

        public static void CompleteFrame()
        {
            if (InputMode != InputModes.WasapiDevice)
                return;
            
            WasapiAudioInput.CompleteFrame();
                
            // Clear bands
            for (int bandIndex = 0; bandIndex < FrequencyBandCount; bandIndex++)
            {
                FrequencyBandPeaks[bandIndex] = MathF.Max(FrequencyBands[bandIndex], FrequencyBandPeaks[bandIndex] * ProjectSettings.Config.AudioDecayFactor); 
                FrequencyBands[bandIndex] = 0;
            }

            int _lastTargetIndex = -1;
            int _lastIndexCount = 0;
            
            for (var i = 0; i < FftBufferSize; i++)
            {
                var gain = FftBuffer[i];
                var gainDb = (gain <= 0.0000001f) ? float.NegativeInfinity : 20 * MathF.Log10(gain); 
                MagnitudeDbB8uffer[i] = MathUtils.RemapAndClamp(gainDb, -96, 0, 0, 1) ;
                    
                var f = (float)i / FftBufferSize;
                var targetIndex = (int)(MathF.Pow(f, 0.3f) * FrequencyBandCount).Clamp(0,FrequencyBandCount-1);
                
                if (targetIndex != _lastTargetIndex)
                {
                    if (_lastTargetIndex >= 0 && _lastIndexCount > 0)
                    {
                        MagnitudeDbB8uffer[_lastTargetIndex] /= _lastIndexCount;
                        _lastIndexCount = 0;
                    }
                    
                    _lastTargetIndex = targetIndex;
                }

                _lastIndexCount++;
                FrequencyBands[targetIndex] += FftBuffer[i];
            }
            
        }

        public static InputModes InputMode = InputModes.Soundtrack;

        public const int FrequencyBandCount = 32;
        public static readonly float[] FrequencyBands = new float[FrequencyBandCount];
        public static readonly float[] FrequencyBandPeaks = new float[FrequencyBandCount];
        
        public static readonly float[] FftBuffer = new float[FftBufferSize];
        public static readonly float[] MagnitudeDbB8uffer = new float[FftBufferSize];
        public static float[] FTTButtonSmoothed;

        public const int BassFlagForFftBufferSize = (int)DataFlags.FFT2048;
        public const int FftBufferSize = 1024; // Half the fft resolution

        public static float AudioLevel;
    }
}