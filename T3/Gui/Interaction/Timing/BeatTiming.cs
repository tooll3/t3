using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.IO;
using T3.Core.Logging;

namespace T3.Gui.Interaction.Timing
{
    /// <summary>
    /// A helper to provide continuous a BeatTime for live situations.
    /// The timing can be driven by BPM detection or beat tapping.
    /// </summary>
    public class BeatTiming2
    {
        public void TriggerSyncTap() => _syncTriggered = true;
        public void TriggerResyncMeasure() => _resyncMeasureTriggered = true;
        public void TriggerDelaySync() => _delayTriggered = true;
        public void TriggerAdvanceSync() => _advanceTriggered = true;

        public static SystemAudioInput SystemAudioInput;

        public float Bpm => (float)(60f / _beatDuration);
        public float DampedBpm => (float)(60f / _dampedBeatDuration);

        public bool UseSystemAudio
        {
            get => _useSystemAudio;
            set
            {
                if (value)
                    InitializeSystemAudio();
                _useSystemAudio = value;
            }
        }

        private bool _useSystemAudio;

        public void SetBpmFromSystemAudio()
        {
            if (SystemAudioInput == null)
            {
                return;
            }

            if (SystemAudioInput.LastIntLevel == 0)
            {
                Log.Warning("Sound seems to be stuck. Trying restart.");
                SystemAudioInput.Restart();
            }

            if (!_bpmDetection.HasSufficientSampleData)
            {
                Log.Warning("Insufficient sample data");
                return;
            }

            var bpm = _bpmDetection.ComputeBpmRate();
            if (!(bpm > 0))
            {
                Log.Warning("Computing bpm-rate failed");
                return;
            }

            Log.Debug("Setting bpm to " + bpm);
            _beatDuration = 60f / bpm;
            _dampedBeatDuration = _beatDuration;
            _lastResyncTime = 0; // Prevent bpm stretching on resync
        }

        public double GetSyncedBeatTiming()
        {
            return SyncedTime;
        }

        public void InitializeSystemAudio()
        {
            if (SystemAudioInput == null)
                SystemAudioInput = new SystemAudioInput();
        }

        public void Update()
        {
            if (_syncTriggered)
            {
            }

            UpdatedBpmDetection();
            UpdateTapping();
        }

        private void UpdatedBpmDetection()
        {
            if (SystemAudioInput == null || _bpmDetection == null)
                return;

            _bpmDetection.AddFftSample(SystemAudioInput.LastFftBuffer);
        }

        /// <remarks>
        /// This code seems much too complicated, but getting flexible and coherent beat detection
        /// seems to be much trickier, than I though. After playing with a couple of methods, it 
        /// finally settled on keeping a "fragmentTime" counter wrapping over the _beatDuration.
        ///     The fragmentTime is than additionally offset with. Maybe there is a method that works
        /// without the separate offset-variable, but since I wanted to have damped transition is
        /// syncing (no jumps please), keeping both separated seemed to work.
        /// </remarks>
        private void UpdateTapping()
        {
            var time = ImGui.GetTime();
            if (Math.Abs(time - _lastTime) < 0.0001f)
                return;

            var timeDelta = time - _lastTime;

            _lastTime = time;

            //var barDuration = _dampedBeatDuration * BeatsPerBar;

            if (_resetTriggered)
            {
                _resetTriggered = false;
                _measureCounter = 0;
                _dampedBeatDuration = 120;
            }

            if (_advanceTriggered)
            {
                _measureStartTime += 0.01f;
                _advanceTriggered = false;
            }

            if (_delayTriggered)
            {
                _measureStartTime -= 0.01f;
                _delayTriggered = false;
            }

            var measureDuration = _beatDuration * BeatsPerMeasure;
            var barDuration = _beatDuration * BeatsPerBar;
            var timeInMeasure = time - _measureStartTime;
            var timeInBar = timeInMeasure % barDuration;

            if (timeInBar > barDuration / 2)
            {
                timeInBar -= barDuration;
            }

            const float precision = 0.5f;
            SyncPrecision = (float)(Math.Max(0, 1 - Math.Abs(timeInBar) / barDuration * 2) - precision) * 1 / (1 - precision);

            if (_resyncMeasureTriggered)
            {
                _resyncMeasureTriggered = false;
                _measureStartTime = time;
            }

            if (_syncTriggered || _resyncMeasureTriggered)
            {
                AddTapAndShiftTimings(time);
            }

            if (_syncTriggered)
            {
                _syncTriggered = false;

                // Fix BPM if completely out of control
                if (double.IsNaN(Bpm) || Bpm < 20 || Bpm > 200)
                {
                    _beatDuration = 1;
                    _tappedMeasureStartTime = 0;
                    _dampedBeatDuration = 0.5;
                    _tapTimes.Clear();
                }

                _tappedMeasureStartTime = time;

                // Stretch _beatTime
                if (Math.Abs(_lastResyncTime) > 0.001f)
                {
                    var timeSinceResync = time - _lastResyncTime;

                    var barCount = timeSinceResync / barDuration;
                    var barCountInt = Math.Round(barCount);
                    var isNotTappingAndNotAbandoned = barCount > 4 && barCount < 128;
                    if (isNotTappingAndNotAbandoned)
                    {
                        var mod = barCount - barCountInt;
                        if (Math.Abs(mod) < 0.5 && barCountInt > 0)
                        {
                            var barFragment = mod * barDuration / barCountInt;
                            var beatShift = barFragment / BeatsPerBar;
                            var newBeatDuration = _beatDuration + beatShift;
                            var bpmChange = Math.Abs(60 / newBeatDuration - 60 / _beatDuration);
                            if (bpmChange > 4)
                            {
                                Log.Debug($"Speed change of {bpmChange:0.0}bpm exceeds threshold. Resync only.");
                            }
                            else
                            {
                                Log.Debug($"Refine from {barCount:0.0}bars :{mod:0.00} shift:{beatShift:0.00} new BPM{(60 / newBeatDuration):0.00}");
                                _beatDuration = newBeatDuration;
                                _measureStartTime = time;
                            }
                        }
                        else
                        {
                            Log.Debug("something");
                        }
                    }
                }
                else
                {
                    Log.Debug("WTF");
                }

                _lastResyncTime = time;
            }

            // Smooth offset and beat duration to avoid jumps
            _dampedBeatDuration = Lerp(_dampedBeatDuration, _beatDuration, 0.03f);

            // Slide start-time to match last beat-trigger
            var tappedTimeInMeasure = time - _tappedMeasureStartTime;
            var differenceToTapping = tappedTimeInMeasure - timeInMeasure;

            var isTimingOff = Math.Abs(differenceToTapping) > 0.03f;
            var slideSpeed = 0.0001f;
            if (isTimingOff)
                _measureStartTime += (differenceToTapping > 0) ? -slideSpeed : slideSpeed;

            _measureStartTime += ProjectSettings.Config.SlideHack;

            // Check for next measure               
            if (timeInMeasure > measureDuration)
            {
                _measureCounter++;
                _measureStartTime += measureDuration;
                timeInMeasure -= measureDuration;
            }

            _tappedMeasureStartTime = time + (_tappedMeasureStartTime - time) % measureDuration;
            _measureProgress = (float)(timeInMeasure / measureDuration);
        }

        private void AddTapAndShiftTimings(double time)
        {
            var newSeriesStarted = _tapTimes.Count == 0 || Math.Abs(time - _tapTimes.Last()) > 4 * _beatDuration;
            if (newSeriesStarted)
                _tapTimes.Clear();

            _tapTimes.Add(time);

            if (_tapTimes.Count < 4)
                return;

            if (_tapTimes.Count > 16)
            {
                _tapTimes.RemoveAt(0);
            }

            var sum = 0.0;
            var lastT = 0.0;

            foreach (var t in _tapTimes)
            {
                if (Math.Abs(lastT) < 0.001f)
                {
                    lastT = t;
                    continue;
                }

                sum += t - lastT;
                lastT = t;
            }

            _beatDuration = sum / (_tapTimes.Count - 1);
        }

        private static double Lerp(double a, double b, float t)
        {
            return a * (1 - t) + b * t;
        }

        private double SyncedTime => (float)(_measureCounter + _measureProgress) * 4;
        private double _lastResyncTime;
        private int _measureCounter;
        private float _measureProgress;

        private double _measureStartTime;

        private double _beatDuration = 0.5;
        private double _dampedBeatDuration = 0.5;

        private const int BeatsPerBar = 4;
        private const int BeatsPerMeasure = 16;
        private double _lastTime;

        private bool _syncTriggered;
        private bool _resetTriggered;

        readonly List<double> _tapTimes = new List<double>();
        private double _tappedMeasureStartTime;
        private BpmDetection _bpmDetection = new BpmDetection();
        public static float SyncPrecision;

        private bool _delayTriggered;
        private bool _advanceTriggered;
        private bool _resyncMeasureTriggered;
    }

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
            
            //time = 0;
            // Fix BPM if completely out of control
            if (double.IsNaN(Bpm) || Bpm < 20 || Bpm > 200)
            {
                _lastBeatDuration = 1;
                _lastPhaseOffset = 0;
                //_dampedBeatDuration = 0.5;
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
                        //var retapQuality = Math.Abs((BeatTime * 4 % 1) - 0.5) * 2;
                        var isTapOnBar = barSync > 0.8;
                        if (isTapOnBar)
                        {
                            var timeSinceFirstTap = Math.Abs(time - _tapTimes[0]);
                            var beatsSinceFirstTap =  Math.Round(timeSinceFirstTap / _lastBeatDuration);
                            _lastBeatDuration = timeSinceFirstTap / beatsSinceFirstTap; 
                            Log.Debug($"Refine attempted detected {isTapOnBar} with {barSync:0.00}");
                        }
                        //_lastPhaseOffset = 0;
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
            
            //Log.Debug($" BeatTiming: dur:{measureDuration:0.00}  phase:{_lastPhaseOffset:0.00} slide:{slideMax:0.000}");
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
            //Log.Debug($"Phase for beat at {timeOfBeat:0.000}  distanceToMeasure:{timeSinceMeasureStart:0.000}  {phase:0.000}");
            return phase;
        }
        

        //public float DampedBpm => Bpm;
        private static double MeasureDuration => _lastBeatDuration * BeatsPerBar * BarsPerMeasure;
        private  const int MaxTapsCount = 16;
        private static readonly List<double> _tapTimes = new List<double>(MaxTapsCount + 1);
        private static int BarsPerMeasure { get; set; } = 4;
        private static int BeatsPerBar { get; set; } = 4;
        //public static float BarSync { get; private static set; }
        private static double _lastBeatDuration = 1;
        private static double _lastPhaseOffset = 0;
        private static double _measureStartTime = 0;
        //private static double _dampedBeatDuration = 0;
        private static double _measureCount = 0;
        private static double _phaseSlideInBarsPerUpdate = 0.001;
        //private static double _lastUpdateTime = 0;
        private static double _syncMeasureOffset = 0;
        private static bool _syncMeasureTriggered;
        private static bool _delayTriggered;
        private static bool _advanceTriggered;
        private static bool _tapTriggered;
    }
}