using T3.Core.Animation;
using T3.Core.Audio;
using T3.Core.DataTypes.DataSet;

//using T3.Core.Utils;

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

        public static float SlideSyncTime;
        private static bool _initialized;

        private static void Initialize()
        {
            if (_initialized)
                return;
            
            DataRecordingBucket.DataSetsById["BeatTiming"] = _syncTimingData;
            _initialized = true;
        }
        public static void Update()
        {
            Initialize();
            
            //BeatTime = (_measureCount + (_runTime - _measureStartTime) / MeasureDuration + _syncMeasureOffset) * BeatsPerBar;

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
            _timingChannel.Events.Add(new DataEvent { Time = _runTime, Value = (float)timeInMeasure%1});
            _bpmChannel.Events.Add(new DataEvent { Time = _runTime, Value = (float)Bpm});
            
            if (timeInMeasure > MeasureDuration)
            {
                _measureCount++;
                _measureStartTime += MeasureDuration;
            }

            if (_syncMeasureTriggered)
            {
                _syncMeasureOffset = -(_runTime - _measureStartTime) / MeasureDuration; // + Playback.Current.BarsFromSeconds(0.03f);
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

            var tInMeasure = (_runTime - _measureStartTime) / MeasureDuration;
            BeatTime = (_measureCount + tInMeasure + _syncMeasureOffset) * BeatsPerBar;

            BeatTimingDetails.Bpm = (float)Bpm;
            BeatTimingDetails.SyncMeasureOffset = (float)_syncMeasureOffset;
            BeatTimingDetails.BeatTime = (float)BeatTime;
            BeatTimingDetails.LastPhaseOffset = (float)_lastPhaseOffset;
        }

        private static void ProcessNewTaps()
        {
            if (!_tapTriggered && !_syncMeasureTriggered)
                return;

            var normalizedMeasureSync = (float)Math.Abs((BeatTime % 4) / 4 - 0.5) * 2;
            var threshold = 0.3;
            if (normalizedMeasureSync < threshold && _syncMeasureTriggered)
            {
                Log.Debug($"Skipping Sync as tap {normalizedMeasureSync:0.00} < {threshold:0.00}");
                return;
            }

            _tapTriggered = false;
            DetectAndProcessOffSeriesTaps(_syncMeasureTriggered);
            KeepTap();
            UpdatePhaseAndDurationFromMultipleTaps();
            ActiveMidiRecording.ActiveRecordingSet ??= new DataSet();
        }

        private static readonly DataChannel _tapsChannel = new(typeof(float)) { Path = new List<string> { "Tapping", "Taps" } };
        private static readonly DataChannel _syncChannel = new(typeof(float)) { Path = new List<string> { "Tapping", "Resync" } };
        private static readonly DataChannel _eventChannel = new(typeof(string)) { Path = new List<string> { "Tapping", "Event" } };
        private static readonly DataChannel _timingChannel = new(typeof(float)) { Path = new List<string> { "Tapping", "Timing" } };
        private static readonly DataChannel _bpmChannel = new(typeof(float)) { Path = new List<string> { "Tapping", "BPM" } };
        private static readonly DataSet _syncTimingData = new() { Channels = new List<DataChannel>() { _tapsChannel, _syncChannel, _eventChannel, _timingChannel, _bpmChannel } };
        
        private static void DetectAndProcessOffSeriesTaps(bool wasResync)
        {
            var normalizedMeasureSync = (float)Math.Abs((BeatTime % 4) / 4 - 0.5) * 2;
            BeatTimingDetails.LastTapBarSync = normalizedMeasureSync;

            if (_tapTimes.Count == 0)
                return;

            var timeSinceLastTap = _runTime - _tapTimes[^1];
            var beatsSinceLastTap = timeSinceLastTap / _lastBeatDuration;

            if (beatsSinceLastTap is < 8 or > 128*16)
            {
                return;
            }
            
            var isTapOnSync = normalizedMeasureSync > 0.8;
            if (!isTapOnSync)
            {
                Log.Debug($"Ignoring offset beat refine (at {normalizedMeasureSync:p1}. Try to sync closer to measure start.");
                _tapTimes.Clear();
                return;
            }
            
            var originalBpm = Bpm;
            var roundBeatMeasureCount = (Math.Round(beatsSinceLastTap/16)*16);
            var newBeatDuration = timeSinceLastTap / roundBeatMeasureCount;
            var newBpm = 60f / newBeatDuration;
            var newBpmIsValid = newBpm > 20f && newBpm < 200f;
            if (wasResync && !newBpmIsValid)
            {
                _tapTimes.Clear();
                Log.Debug("Ignoring resync for bpm measure");
                return;
            }

            _lastBeatDuration = newBeatDuration;
            Log.Debug($"Refining BPM rate {originalBpm:0.0} -> {Bpm:0.0}.  ({beatsSinceLastTap:0.0} {roundBeatMeasureCount}beats  over {timeSinceLastTap:0.00s}s)");

            // Also shift resync
            _syncMeasureOffset = -(_runTime - _measureStartTime) / MeasureDuration;
            _tapTimes.Clear();
        }

        private static void KeepEvent(string message)
        {
            _eventChannel.Events.Add(new DataEvent
                                        {
                                            Time = Playback.RunTimeInSecs,
                                            TimeCode = Playback.RunTimeInSecs,
                                            Value = message
                                        });
        }

        private static void KeepTap()
        {
            _tapsChannel.Events.Add(new DataEvent
                                        {
                                            Time = Playback.RunTimeInSecs,
                                            TimeCode = Playback.RunTimeInSecs,
                                            Value = 100f
                                        });
            
            while (_tapTimes.Count > MaxTapsCount)
            {
                _tapTimes.RemoveAt(0);
            }

            _tapTimes.Add(_runTime);
        }

        private static void UpdatePhaseAndDurationFromMultipleTaps()
        {
            if (_tapTimes.Count < 4)
            {
                _lastPhaseOffset = 0;
                return;
            }

            var lastTapTime = _tapTimes[^1];
            if (_runTime - lastTapTime > 4)
            {
                _lastPhaseOffset = 0;
                _tapTimes.Clear();
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
        private static readonly List<double> _tapTimes = new(MaxTapsCount + 1);
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