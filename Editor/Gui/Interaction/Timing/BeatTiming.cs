using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.Logging;

namespace T3.Editor.Gui.Interaction.Timing
{
    /// <summary>
    /// A helper to provide continuous a BeatTime for live situations.
    /// The timing can be driven by BPM detection or beat tapping.
    ///
    /// Its result is used by <see cref="BeatTimingPlayback"/> to drive playback. 
    /// </summary>
    public static class BeatTiming
    {
        public static double BeatTime { get; private set; }
        public static float Bpm => (float)(60f / _lastBeatDuration);

        public static void SetBpmRate(float bpm)
        {
            _lastBeatDuration = 60f / bpm;
        }

        public static void TriggerSyncTap() => _tapTriggered = true;
        public static void TriggerDelaySync() => _delayTriggered = true;
        public static void TriggerAdvanceSync() => _advanceTriggered = true;
        public static void TriggerResyncMeasure() => _syncMeasureTriggered = true;

        public static void Update(double runTime2)
        {
            _runTime = Playback.RunTimeInSecs;
            
            BeatTimingDetails.WasResyncTriggered = _syncMeasureTriggered ? 1 : 0;
            BeatTimingDetails.WasTapTriggered = _tapTriggered ? 1 : 0;

            // Fix BPM if completely out of control
            if (double.IsNaN(Bpm) || Bpm < 20 || Bpm > 200)
            {
                Log.Warning($"BPM {Bpm:0.0} out of range. Reverting to 120");
                _lastBeatDuration = 0.5; // 120bpm
                _lastPhaseOffset = 0;
                _syncMeasureOffset = 0;
                _tapTimes.Clear();
            }

            ProcessNewTaps();

            // Check for next measure               
            var timeInMeasure = _runTime - _measureStartTime;
            if (timeInMeasure > MeasureDuration)
            {
                _measureCount++;
                _measureStartTime += MeasureDuration;
            }

            if (_syncMeasureTriggered)
            {
                _syncMeasureOffset = -(_runTime - _measureStartTime) / MeasureDuration;
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
            var slideMax = Math.Min(PhaseSlideInBarsPerUpdate, Math.Abs(_lastPhaseOffset));
            if (_lastPhaseOffset > 0)
            {
                _measureStartTime += slideMax;
            }
            else
            {
                _measureStartTime -= slideMax;
            }

            BeatTime = (_measureCount + (_runTime - _measureStartTime) / MeasureDuration + _syncMeasureOffset) * BeatsPerBar;

            BeatTimingDetails.Bpm = (float)Bpm;
            BeatTimingDetails.SyncMeasureOffset = (float)_syncMeasureOffset;
            BeatTimingDetails.BeatTime = (float)BeatTime;
            BeatTimingDetails.LastPhaseOffset = (float)_lastPhaseOffset;
        }

        private static void ProcessNewTaps()
        {
            if (!_tapTriggered && !_syncMeasureTriggered)
                return;
            
            _tapTriggered = false;

            DetectAndProcessOffSeriesTaps();
            KeepTap();
            UpdatePhaseAndDurationFromMultipleTaps();
        }

        
        private static void KeepTap()
        {
            while (_tapTimes.Count > MaxTapsCount)
            {
                _tapTimes.RemoveAt(0);
            }
            
            _tapTimes.Add(_runTime);
        }
        
        
        private static void DetectAndProcessOffSeriesTaps()
        {
            var normalizedMeasureSync = (float)Math.Abs((BeatTime % 4)/4 - 0.5) * 2;            
            BeatTimingDetails.LastTapBarSync = normalizedMeasureSync;

            if (_tapTimes.Count == 0)
                return;
            
            var beatsSinceLastTap = Math.Abs(_runTime - _tapTimes.Last()) / _lastBeatDuration;
            
            if (!(beatsSinceLastTap > 8) || !(beatsSinceLastTap < 128))
                return;
            
            var isTapOnSync = normalizedMeasureSync > 0.8;
            if (!isTapOnSync)
            {
                Log.Debug($"Ignoring offset beat refine (at {normalizedMeasureSync:p1}. Try to sync closer to measure start.");
                _tapTimes.Clear();
                _tapTimes.Add(_runTime);
                return;
            }

                    
            var timeSinceFirstTap = Math.Abs(_runTime - _tapTimes[0]);
            var beatsSinceFirstTap = Math.Round(timeSinceFirstTap / _lastBeatDuration);
                    
            var originalBpm = Bpm;
            _lastBeatDuration = timeSinceFirstTap / beatsSinceFirstTap;
            Log.Debug($"Refining BPM rate {originalBpm:0.0} -> {Bpm:0.0}.  ({beatsSinceFirstTap:0.0} over {timeSinceFirstTap:0.00s})");

            // Also shift resync
            _syncMeasureOffset = -(_runTime - _measureStartTime) / MeasureDuration;
            _tapTimes.Clear();
            _tapTimes.Add(_runTime);
        }
        
        private static void UpdatePhaseAndDurationFromMultipleTaps()
        {
            if (_tapTimes.Count < 4)
            {
                _lastPhaseOffset = 0;
                return;
            }

            var lastTapTime = _tapTimes[_tapTimes.Count - 1];
            if (_runTime - lastTapTime > 4)
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

                phaseOffsetSum += GetPhaseOffsetForBeatTap(t);
                durationSum += dt;
            }

            var stepCount = _tapTimes.Count - 1;
            _lastBeatDuration = durationSum / stepCount;
            _lastPhaseOffset = phaseOffsetSum / stepCount;
        }

        private static double GetPhaseOffsetForBeatTap(double timeOfBeat)
        {
            var measureTime = _measureStartTime + MeasureDuration;
            var timeSinceMeasureStart = measureTime - timeOfBeat;
            var beatCount = Math.Abs(timeSinceMeasureStart) / _lastBeatDuration;
            var beatCountRounded = Math.Round(beatCount);
            var beatAlignment = measureTime - (_lastBeatDuration * beatCountRounded);
            var phase = (timeOfBeat - beatAlignment) * -1;
            return phase;
        }

        private static double MeasureDuration => _lastBeatDuration * BeatsPerBar * BarsPerMeasure;
        private const int MaxTapsCount = 16;
        private static readonly List<double> _tapTimes = new List<double>(MaxTapsCount + 1);
        private static int BarsPerMeasure { get; set; } = 4;
        private static int BeatsPerBar { get; set; } = 4;
        
        private static double _lastBeatDuration = 1;

        private static double _lastPhaseOffset;
        private static double _measureStartTime;
        private static double _measureCount;
        private const double PhaseSlideInBarsPerUpdate = 0.001;
        private static double _syncMeasureOffset;
        private static bool _syncMeasureTriggered;
        private static bool _delayTriggered;
        private static bool _advanceTriggered;
        private static bool _tapTriggered;
        private static double _runTime;
    }
}