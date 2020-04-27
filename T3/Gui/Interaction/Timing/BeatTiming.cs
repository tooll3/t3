using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using T3.Core.Logging;

namespace T3.Gui.Interaction.Timing
{
    /// <summary>
    /// A helper to provide continuous a BeatTime for live si tuations.
    /// The timing can be driven by BPM detection or beat tapping.
    /// </summary>
    /// <remarks>
    /// The code is partly inspired by early work on tooll.io</remarks>
    public class BeatTiming
    {
        public void TriggerSyncTap() => _syncTriggered = true;
        public void TriggerReset() =>   _resetTriggered = true;

        public float ComputeBpmFromSystemAudio()
        {
            if (_bpmDetection.HasSufficientSampleData)
            {
                var bpm = _bpmDetection.ComputeBpmRate();
                Log.Debug("New Bpm would be " + bpm);
                return bpm;
            }
            Log.Warning("Insufficient sample data");
            return -1;
        } 
        
        public double GetSyncedBeatTiming()
        {
            if(_systemAudioInput == null)
                _systemAudioInput = new SystemAudioInput();
            
            return SyncedTime;
        }

        private static SystemAudioInput _systemAudioInput;
        
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
            if (_systemAudioInput == null || _bpmDetection == null)
                return;

            _bpmDetection.AddFffSample(_systemAudioInput.LastFftBuffer);
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

            //var timeDelta = time - _lastTime;
            _lastTime = time;

            var barDuration = _dampedBeatDuration * BeatsPerBar;

            if (_resetTriggered)
            {
                _resetTriggered = false;
                _barCounter = 0;
                _dampedBeatDuration = 120;
            }

            if (_syncTriggered)
            {
                _syncTriggered = false;
                AddTapAndShiftTimings(time);

                // Fix BPM if completely out of control
                var bpm = 60f / _beatDuration;
                if (double.IsNaN(bpm) || bpm < 20 || bpm > 200)
                {
                    _beatDuration = 1;
                    _tappedBarStartTime = 0;
                    _dampedBeatDuration = 0.5;
                    _tappedBarStartTime = 0;
                    _tapTimes.Clear();
                }

                var measureDuration = _beatDuration * BeatsPerBar / 4;
                var timeInMeasure = time - _barStartTime;

                if (timeInMeasure > measureDuration / 2)
                {
                    timeInMeasure -= measureDuration;
                }

                if (Math.Abs(timeInMeasure) > measureDuration / BeatsPerBar)
                {
                    // Reset measure-timing
                    _tappedBarStartTime = _barStartTime = time;
                }
                else
                {
                    // Sliding is sufficient
                    _tappedBarStartTime = time;
                }

                // Stretch _beatTime
                if (Math.Abs(_lastResyncTime) > 0.001f)
                {
                    var timeSinceResync = time - _lastResyncTime;

                    var measureCount = timeSinceResync / measureDuration;
                    var measureCountInt = Math.Round(measureCount);
                    Log.Debug("MeasureCount:" + measureCount);
                    var mod = measureCount - measureCountInt;
                    Log.Debug(" Mod:" + mod);
                    if (Math.Abs(mod) < 0.5 && measureCountInt > 0)
                    {
                        var measureFragment = mod * measureDuration / measureCountInt;
                        var beatShift = measureFragment / BeatsPerBar;
                        _beatDuration += beatShift;
                        Log.Debug("Resync-Offset:" + mod + " shift:" + beatShift + " new BPM" + (60 / _beatDuration));
                    }
                }

                _lastResyncTime = time;
            }

            // Smooth offset and beatduration to avoid jumps
            _dampedBeatDuration = Lerp(_dampedBeatDuration, _beatDuration, 0.05f);

            // Slide start-time to match last beat-trigger
            var timeInBar = time - _barStartTime;
            var tappedTimeInBar = time - _tappedBarStartTime;
            var tappedBeatTime = (tappedTimeInBar / _dampedBeatDuration) % 1f;
            var beatTime = (timeInBar / _dampedBeatDuration) % 1f;

            _barStartTime += (beatTime < tappedBeatTime) ? -0.01f : 0.01f;

            // Check for next bar               
            if (timeInBar > barDuration)
            {
                _barCounter++;
                _barStartTime += barDuration;
                timeInBar -= barDuration;
            }

            _tappedBarStartTime = time + (_tappedBarStartTime - time) % barDuration;
            _barProgress = (float)(timeInBar / barDuration);
        }

        private void AddTapAndShiftTimings(double time)
        {
            var newSeriesStarted = _tapTimes.Count == 0 || Math.Abs(time - _tapTimes.Last()) > 16 * _beatDuration;
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
            _tappedBarStartTime = time - _beatDuration;
        }
        
        
        private static double Lerp(double a, double b, float t)
        {
            return a * (1 - t) + b * t;
        }
        
        // private float _beatTime = 0;
        // private float _tapTime = 0;
        private double SyncedTime => (float)(_barCounter + _barProgress) * 4;
        private double _lastResyncTime;
        private int _barCounter;
        private float _barProgress;

        private double _barStartTime;

        private double _beatDuration = 0.5;
        private double _dampedBeatDuration = 0.5;

        
        private const float BeatsPerBar = 16;
        private double _lastTime;
        
        private bool _syncTriggered;
        private bool _resetTriggered;

        readonly List<double> _tapTimes = new List<double>();
        private double _tappedBarStartTime;
        private BpmDetection _bpmDetection = new BpmDetection();
    }
}