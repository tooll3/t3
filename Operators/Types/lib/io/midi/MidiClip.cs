using System;
using System.Collections.Generic;
using System.Linq;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using NAudio.Midi;


namespace T3.Operators.Types.Id_a3ceb788_4055_4556_961b_63b7221f93e7
{
    public class MidiClip : Instance<MidiClip>, IDisposable
    {
        [Output(Guid = "4771E114-58CB-4944-910D-20D9DE5F2367", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Dict<float>> Values = new();

        [Output(Guid = "CE9BF60F-F43A-431C-8715-CF3A14593DB3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> Tracks = new();

        [Output(Guid = "AADD9189-0086-42D6-AC45-D694270C0252", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> DeltaTicksPerQuarterNote = new();

        [Output(Guid = "A5250FBA-092F-48C9-A979-A88FA3A793B1", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly TimeClipSlot<Command> TimeSlot = new();

        public MidiClip()
        {
            _initialized = false;

            TimeSlot.UpdateAction = Update;
            Tracks.UpdateAction = Update;
            DeltaTicksPerQuarterNote.UpdateAction = Update;
            Values.UpdateAction = Update;
        }

        protected override void Dispose(bool isDisposing)
        {
            //if (!isDisposing)
            //    return;
        }

        private void Update(EvaluationContext context)
        {
            try
            {
                if (!_initialized || Filename.DirtyFlag.IsDirty)
                {
                    SetupMidiFile(context);
                }

                if (_midiEventCollection == null) 
                    return;

                _printLogMessages = PrintLogMessages.GetValue(context);

                // Get scaled time range of clip
                var timeRange = TimeSlot.TimeClip.TimeRange;
                var sourceRange = TimeSlot.TimeClip.SourceRange;

                // Get the time we should be at in the MIDI file according to the timeClip
                var bars = context.LocalTime - timeRange.Start;
                if (Math.Abs(timeRange.End - timeRange.Start) > 0.0001f)
                {
                    var rate = (sourceRange.End - sourceRange.Start)
                               / (timeRange.End - timeRange.Start);
                    bars *= rate;
                }

                bars += sourceRange.Start;

                // For now: brute-force rewind if we run backwards in time
                if (bars < _lastTimeInBars)
                    ClearTracks();

                _lastTimeInBars = bars;

                // Include past events in our response
                var minRange = Math.Min(sourceRange.Start, sourceRange.End);
                if (bars >= minRange &&
                    bars < minRange + Math.Abs(sourceRange.Duration))
                {
                    for (var trackIndex = 0; trackIndex < _midiFile.Tracks; trackIndex++)
                    {
                        UpdateTrack(trackIndex, bars);
                    }
                }

                if (_valuesUpdated)
                    Values.Value = _values;
            }
            catch (Exception e)
            {
                Log.Debug("Updating MidiClip failed:" + e.Message, SymbolChildId);
            }
        }

        private void SetupMidiFile(EvaluationContext context)
        {
            var filename = Filename.GetValue(context);
            if (string.IsNullOrEmpty(filename))
                return;
            
            // Initialize MIDI file reading, then read all parameters from file
            const bool noStrictMode = false;
            _midiFile = new MidiFile(filename, noStrictMode);
            _deltaTicksPerQuarterNote = _midiFile.DeltaTicksPerQuarterNote;
            _midiEventCollection = _midiFile.Events;
            _lastTrackEventIndices = Enumerable.Repeat(-1, _midiFile.Tracks).ToList();
            _timeSignature = _midiFile.Events[0].OfType<TimeSignatureEvent>().FirstOrDefault();

            // Update slots
            Tracks.Value = _midiFile.Tracks;
            DeltaTicksPerQuarterNote.Value = (int)_deltaTicksPerQuarterNote;    // conversion to int is probably bad

            _initialized = true;
        }

        private void ClearTracks()
        {
            for (var index = 0; index < _lastTrackEventIndices.Count; index++)
            {
                _lastTrackEventIndices[index] = -1;
            }

            foreach (var k in _values.Keys)
            {
                _values[k] = 0;
            }
        }

        private void UpdateTrack(int trackIndex, double time)
        {
            _valuesUpdated = false;

            if (trackIndex >= _lastTrackEventIndices.Count ||
                trackIndex >= _midiFile.Events[trackIndex].Count) 
                return;

            var events = _midiFile.Events[trackIndex];
            
            var lastEventIndex = _lastTrackEventIndices[trackIndex];
            if (lastEventIndex + 1 >= events.Count) 
                return;

            var timeInTicks = (long)(time * 4 * _deltaTicksPerQuarterNote);
            var nextEventIndex = lastEventIndex + 1;
            while (nextEventIndex < events.Count
                   && timeInTicks >= events[nextEventIndex].AbsoluteTime)
            {
                if (_printLogMessages)
                {
                    Log.Debug(ToBarsBeatsTicks(events[nextEventIndex].AbsoluteTime,
                                    _deltaTicksPerQuarterNote, _timeSignature));
                }

                switch (events[nextEventIndex])
                {
                    case NoteOnEvent noteOnEvent:
                    {
                        var channel = noteOnEvent.Channel;
                        var name = noteOnEvent.NoteName;
                        var value = noteOnEvent.Velocity / 127f;
                        var key = $"/channel{channel}/{name}";
                        _values[key] = value;
                        _valuesUpdated = true;

                        if (_printLogMessages)
                            Log.Debug(key + "=" + value);
                        break;
                    }
                    case NoteEvent noteEvent:
                    {
                        var channel = noteEvent.Channel;
                        var name = noteEvent.NoteName;
                        const float value = 0.0f;
                        var key = $"/channel{channel}/{name}";
                        _values[key] = value;
                        _valuesUpdated = true;

                        if (_printLogMessages)
                            Log.Debug(key + "=" + value);
                        break;
                    }
                    case ControlChangeEvent controlChangeEvent:
                    {
                        var channel = controlChangeEvent.Channel;
                        var controller = (int)controlChangeEvent.Controller;
                        var value = controlChangeEvent.ControllerValue / 127f;
                        var key = $"/channel{channel}/controller{controller}";
                        _values[key] = value;
                        _valuesUpdated = true;

                        if (_printLogMessages)
                            Log.Debug($"{key}={value}");
                        
                        break;
                    }
                }

                lastEventIndex = nextEventIndex;
                nextEventIndex = lastEventIndex + 1;
            }

            _lastTrackEventIndices[trackIndex] = lastEventIndex;
        }

        /**
         * From https://github.com/naudio/NAudio/blob/master/Docs/MidiFile.md
         */
        private static string ToBarsBeatsTicks(long eventTime, double ticksPerQuarterNote, TimeSignatureEvent timeSignature)
        {
            var beatsPerBar = timeSignature?.Numerator ?? 4;
            var ticksPerBar = timeSignature == null
                                  ? ticksPerQuarterNote * 4
                                  : (timeSignature.Numerator * ticksPerQuarterNote * 4) / (1 << timeSignature.Denominator);
            var ticksPerBeat = ticksPerBar / beatsPerBar;
            var bar = (eventTime / ticksPerBar);
            var beat = ((eventTime % ticksPerBar) / ticksPerBeat);
            var tick = eventTime % ticksPerBeat;
            return string.Format($"{bar}:{beat}:{tick}");
        }

        // The MIDI file input
        private bool _initialized = false;
        private MidiFile _midiFile = null;
        private MidiEventCollection _midiEventCollection = null;
        private double _deltaTicksPerQuarterNote = 500000.0 / 60;

        // Output data
        private readonly Dict<float> _values = new(0f);

        // Parsing the file
        private TimeSignatureEvent _timeSignature = null;
        private double _lastTimeInBars = 0f;
        private List<int> _lastTrackEventIndices = null;
        private bool _printLogMessages = false;
        private bool _valuesUpdated = false;

        // [Input(Guid = "71655B86-B0ED-422E-89E3-E678C87A7E0E")]
        // Public readonly InputSlot<T3.Core.Command> Command = new();

        [Input(Guid = "31FE831F-C3BE-4AE3-884B-D2FC4F1754A4")]
        public readonly InputSlot<string> Filename = new();

        [Input(Guid = "8B88C669-7351-4332-9294-9A06A46F45A1")]
        public readonly InputSlot<bool> PrintLogMessages = new();
    }
}