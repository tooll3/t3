using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;

namespace T3.Operators.Types.Id_1fa651c8_ab73_4ca0_9506_84602bbf2fcb
{
    /// <summary>
    /// This is an older implementation. A slighted updated
    /// algorithm can be found in <see cref="Editor.Gui.Interaction.Timing.BpmDetection"/>
    /// </summary>
    public class DetectBeatOffset : Instance<DetectBeatOffset>
    {
        [Output(Guid = "c53fb442-4dd7-473c-8a1f-1adaabe3bcf7")]
        public readonly Slot<List<float>> Measurements = new();
        
        public DetectBeatOffset()
        {
            Measurements.UpdateAction = Update;
        }
        
        private class TimeEntry
        {
            public double Time;
            public float[] FftValues;
        }

        private double _lastEvalTime;


        private List<float[]> _slicesForSmoothing = new List<float[]>();
        
        private void Update(EvaluationContext context)
        {
            var fft = FftInput.GetValue(context);
            if (fft == null || fft.Count == 0)
            {
                return;
            }

            var timeSinceLastEval = Playback.RunTimeInSecs - _lastEvalTime;
            if (timeSinceLastEval < 0.01)
                return;
            
            _lastEvalTime = Playback.RunTimeInSecs;

            const float assumedFrameCount = 10f;
            var timeSinceLastUpdate = Playback.LastFrameDuration;
            var fillCount =  (int)Math.Round(timeSinceLastUpdate / (1 / assumedFrameCount) - 0.5f) + 1; 
            
            var historyDurationInSecs = 20f;
            var maxFftHistoryLength =  (int)(assumedFrameCount * historyDurationInSecs);

            var currentTime = context.Playback.TimeInSecs;
            var maxValueCount = 20;

            if (fft.Count < maxValueCount)
                return;
            
            // Down sample
            var values = new float[maxValueCount];
            var delta = (fft.Count / (float)maxValueCount);
            var sourceIndex = 0f;
            for (var valueIndex = 0; valueIndex < maxValueCount && sourceIndex <= fft.Count; valueIndex++)
            {
                values[valueIndex] = fft[(int)sourceIndex];
                sourceIndex += delta;
            }

            const int smoothWindow = 3;
            _slicesForSmoothing.Insert(0, values);
            if(_slicesForSmoothing.Count >= smoothWindow)
                _slicesForSmoothing.RemoveRange(smoothWindow , _slicesForSmoothing.Count - smoothWindow );
            
            // Smooth
            var smoothedValues = new float[maxValueCount];
            for (var valueIndex = 0; valueIndex < maxValueCount; valueIndex++)
            {
                var s = 0f;
                for (var smoothIndex = 0; smoothIndex < _slicesForSmoothing.Count; smoothIndex++)
                {
                    s += _slicesForSmoothing[smoothIndex][valueIndex];
                }
                smoothedValues[valueIndex] = s / _slicesForSmoothing.Count;
            }
            
            _fftHistory.Insert(0, new TimeEntry()
                                      {
                                          Time= currentTime,
                                          FftValues= smoothedValues,
                                      });
            
            // Clamp length of buffer            
            if(_fftHistory.Count >= maxFftHistoryLength)
                _fftHistory.RemoveRange(maxFftHistoryLength , _fftHistory.Count - maxFftHistoryLength);
            
            var sliceCountForBar = (int)(Playback.Current.SecondsFromBars(8) * assumedFrameCount);
            
            Log.Debug($"Slice count {sliceCountForBar}");
            var shiftCount = 50;
            
            _results.Clear();
            var min = float.PositiveInfinity;
            var max = float.NegativeInfinity;
            for (var shiftIndex = - shiftCount / 2; shiftIndex < shiftCount /2; shiftIndex++)
            {
                var energyDifference = GetEnergyDifference(shiftIndex + sliceCountForBar, sliceCountForBar * 2);
                min = MathF.Min(energyDifference, min);
                max = MathF.Max(energyDifference, max);
                _results.Add(energyDifference );
            }

            for (var index = 0; index < _results.Count; index++)
            {
                _results[index] = MathF.Pow(MathUtils.RemapAndClamp(_results[index], min, max, 0,1),2);
            }

            Measurements.Value = _results;
        }

        private List<float> _results = new();

        private float GetEnergyDifference(int offset, int sliceLength)
        {
            float energyDifference = 0;

            for (var sliceIndex = 0; sliceIndex < sliceLength; sliceIndex++)
            {
                if (sliceIndex >= _fftHistory.Count)
                {
                    continue;
                }
                
                int shiftedIndex = sliceIndex + offset;
                if (shiftedIndex < 0 || shiftedIndex >= _fftHistory.Count)
                {
                    continue;
                }

                var valuesNew = _fftHistory[sliceIndex].FftValues;
                var valuesOld = _fftHistory[shiftedIndex].FftValues;
                var sliceWidth = Math.Min(valuesNew.Length, valuesOld.Length);
                if (sliceWidth == 0)
                {
                    continue;
                }

                for (var valueIndex = 0; valueIndex < sliceWidth; valueIndex++)
                {
                    var diff = MathF.Abs(valuesNew[valueIndex] - valuesOld[valueIndex]);
                    //var energy = 1 / (diff + 0.1f);
                    var energy = MathF.Pow(diff, 4); 
                    energyDifference += energy;
                    //energyDifference = MathF.Pow() * 100,2f) / sliceWidth;
                }
            }

            return 1/ energyDifference;
        }
        
        private readonly List<TimeEntry> _fftHistory = new();
        
        [Input(Guid = "09628f30-9148-477e-a375-a7e661952ee5")]
        public readonly InputSlot<List<float>> FftInput = new(new List<float>(20));
    }
}