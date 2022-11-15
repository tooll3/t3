using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Logging;

namespace Editor.Gui.Interaction.Timing
{
    /// <summary>
    /// A helper to provide continuous a BeatTime for live situations.
    /// The timing can be driven by BPM detection or beat tapping.
    /// </summary>
    public static class BeatTiming
    {
        public static double BeatTime { get; private set; }
        public static float Bpm => (float)(60f / _lastBeatDuration);

        public static void TriggerSyncTap() => _tapTriggered = true;
        public static void TriggerDelaySync() => _delayTriggered = true;
        public static void TriggerAdvanceSync() => _advanceTriggered = true;
        public static void TriggerResyncMeasure() => _syncMeasureTriggered = true;

        public static void Update(double time)
        {
            // Fix BPM if completely out of control
            if (double.IsNaN(Bpm) || Bpm < 20 || Bpm > 200)
            {
                _lastBeatDuration = 1;
                _lastPhaseOffset = 0;
                _lastPhaseOffset = 0;
                _tapTimes.Clear();
            }
            
            var barSync = (float)Math.Abs((BeatTime % 1) - 0.5) * 2;
            
            if (_tapTriggered)
            {
                // A "series" is a sequence to tapping once on every beat
                if (_tapTimes.Count > 0)
                {
                    var beatsSinceLastTap = Math.Abs(time - _tapTimes.Last()) / _lastBeatDuration;
                    if (beatsSinceLastTap> 4 && beatsSinceLastTap < 128)
                    {
                        var isTapOnBar = barSync > 0.8;
                        if (isTapOnBar)
                        {
                            var timeSinceFirstTap = Math.Abs(time - _tapTimes[0]);
                            var beatsSinceFirstTap =  Math.Round(timeSinceFirstTap / _lastBeatDuration);
                            _lastBeatDuration = timeSinceFirstTap / beatsSinceFirstTap; 
                            Log.Debug($"Refine attempted detected with {barSync:0.00}");
                        }
                        _tapTimes.Clear();
                    }
                }

                _tapTimes.Add(time);

                while (_tapTimes.Count > MaxTapsCount)
                {
                    _tapTimes.RemoveAt(0);
                }

                _tapTriggered = false;
            }

            UpdatePhaseAndDuration(time);

            // Check for next measure               
            var timeInMeasure = time - _measureStartTime;
            var measureDuration = _lastBeatDuration * BeatsPerBar * BarsPerMeasure;
            if (timeInMeasure > measureDuration)
            {
                _measureCount++;
                _measureStartTime += measureDuration;
            }

            if (_syncMeasureTriggered)
            {
                _syncMeasureOffset = -(time - _measureStartTime) / measureDuration;
                _syncMeasureTriggered = false;
            }

            if (_advanceTriggered)
            {
                _syncMeasureOffset += 0.01;
                _advanceTriggered = false;
            }

            if (_delayTriggered)
            {
                _syncMeasureOffset -= 0.01;
                _delayTriggered = false;
            }



            // Slide phase
            var slideMax = Math.Min(_phaseSlideInBarsPerUpdate, Math.Abs(_lastPhaseOffset));
            if (_lastPhaseOffset > 0)
            {
                _measureStartTime += slideMax;
            }
            else
            {
                _measureStartTime -= slideMax;
            }

            BeatTime = (_measureCount + (time - _measureStartTime) / measureDuration + _syncMeasureOffset) * BeatsPerBar;
        }

        private static void UpdatePhaseAndDuration(double time)
        {
            if (_tapTimes.Count < 4)
            {
                _lastPhaseOffset = 0;
                return;
            }

            var lastTapTime = _tapTimes[_tapTimes.Count - 1];
            if (time - lastTapTime > 4)
            {
                _lastPhaseOffset = 0;
                return;
            }

            var durationSum = 0.0;
            var phaseOffsetSum = 0.0;
            var lastT = 0.0;

            for (var tapIndex = 0; tapIndex < _tapTimes.Count; tapIndex++)
            {
                var t = _tapTimes[tapIndex];
                var dt = t - lastT;

                lastT = t;
                if (tapIndex == 0)
                    continue;

                phaseOffsetSum += GetPhaseOffsetForBeat(t);
                durationSum += dt;
            }

            var stepCount = _tapTimes.Count - 1;
            _lastBeatDuration = durationSum / stepCount;
            _lastPhaseOffset = phaseOffsetSum / stepCount;
        }

        private static double GetPhaseOffsetForBeat(double timeOfBeat)
        {
            var measureTime = _measureStartTime + MeasureDuration;
            var timeSinceMeasureStart =    measureTime- timeOfBeat;
            var beatCount = Math.Abs(timeSinceMeasureStart ) / _lastBeatDuration;
            var beatCountRounded = Math.Round(beatCount);
            var beatAlignment = measureTime - (_lastBeatDuration * beatCountRounded);
            var phase = (timeOfBeat - beatAlignment) * -1;
            return phase;
        }
        
        private static double MeasureDuration => _lastBeatDuration * BeatsPerBar * BarsPerMeasure;
        private  const int MaxTapsCount = 16;
        private static readonly List<double> _tapTimes = new List<double>(MaxTapsCount + 1);
        private static int BarsPerMeasure { get; set; } = 4;
        private static int BeatsPerBar { get; set; } = 4;
        private static double _lastBeatDuration = 1;
        private static double _lastPhaseOffset;
        private static double _measureStartTime;
        private static double _measureCount;
        private static double _phaseSlideInBarsPerUpdate = 0.001;
        private static double _syncMeasureOffset;
        private static bool _syncMeasureTriggered;
        private static bool _delayTriggered;
        private static bool _advanceTriggered;
        private static bool _tapTriggered;
    }
}