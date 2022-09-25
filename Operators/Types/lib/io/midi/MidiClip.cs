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
using MF = NAudio.Midi.MidiFile;


namespace T3.Operators.Types.Id_a3ceb788_4055_4556_961b_63b7221f93e7
{
    public class MidiClip : Instance<MidiClip>, IDisposable
    {
        #region outputs
        [Output(Guid = "4771E114-58CB-4944-910D-20D9DE5F2367", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Dict<float>> Values = new();

        [Output(Guid = "CE9BF60F-F43A-431C-8715-CF3A14593DB3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> Tracks = new();

        [Output(Guid = "AADD9189-0086-42D6-AC45-D694270C0252", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> DeltaTicksPerQuarterNote = new();

        [Output(Guid = "A5250FBA-092F-48C9-A979-A88FA3A793B1", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly TimeClipSlot<Command> TimeSlot = new();
        #endregion

        [Input(Guid = "71655B86-B0ED-422E-89E3-E678C87A7E0E")]
        public readonly InputSlot<T3.Core.Command> Command = new();

        [Input(Guid = "31FE831F-C3BE-4AE3-884B-D2FC4F1754A4")]
        public readonly InputSlot<string> Filename = new();

        [Input(Guid = "8B88C669-7351-4332-9294-9A06A46F45A1")]
        public readonly InputSlot<bool> PrintLogMessages = new();

        public MidiClip()
        {
            _initialized = false;
            _values = new Dict<float>(0f);

            TimeSlot.UpdateAction = Update;
            Tracks.UpdateAction = Update;
            DeltaTicksPerQuarterNote.UpdateAction = Update;
            Values.UpdateAction = Update;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;
        }

        private void Update(EvaluationContext context)
        {
            try
            {
                Command.GetValue(context);

                if (!_initialized || Filename.DirtyFlag.IsDirty)
                {
                    SetupMidiFile(context);
                }

                if (_midiEventCollection == null) return;

                _printLogMessages = PrintLogMessages.GetValue(context);

                // get scaled time range of clip
                var timeRange = TimeSlot.TimeClip.TimeRange;
                var sourceRange = TimeSlot.TimeClip.SourceRange;

                // get the time we should be at in the MIDI file according to the timeClip
                var bars = context.LocalTime - timeRange.Start;
                if (timeRange.End != timeRange.Start)
                {
                    var rate = (sourceRange.End - sourceRange.Start)
                             / (timeRange.End - timeRange.Start);
                    bars *= rate;
                }
                bars += sourceRange.Start;

                // for now: brute-force rewind if we run backwards in time
                if (bars < _lastTimeInBars)
                    ClearTracks();

                _lastTimeInBars = bars;

                // include past events in our response
                var minRange = Math.Min(sourceRange.Start, sourceRange.End);
                if (bars >= minRange &&
                    bars < minRange + Math.Abs(sourceRange.Duration))
                {
                    for (var n = 0; n < _midiFile.Tracks; n++)
                    {
                        UpdateTrack(n, bars);
                    }
                }

                if (_valuesUpdated)
                    Values.Value = _values;
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
            }
        }

        private void SetupMidiFile(EvaluationContext context)
        {
            var strictMode = false;
            var filename = Filename.GetValue(context);
            if (filename != null && filename.Length != 0)
            {
                // initialize MIDI file reading, then read all parameters from file
                _midiFile = new MF(filename, strictMode);
                _deltaTicksPerQuarterNote = _midiFile.DeltaTicksPerQuarterNote;
                _midiEventCollection = _midiFile.Events;
                _lastEventOfTrack = Enumerable.Repeat(-1, _midiFile.Tracks).ToList();
                _timeSignature = _midiFile.Events[0].OfType<TimeSignatureEvent>().FirstOrDefault();

                // update slotds
                Tracks.Value = _midiFile.Tracks;
                DeltaTicksPerQuarterNote.Value = _deltaTicksPerQuarterNote;

                _initialized = true;
            }
        }

        private void ClearTracks()
        {
            _lastEventOfTrack = Enumerable.Repeat(-1, _midiFile.Tracks).ToList();

            // set all previously acquired values to 0
            var keys = _values.Keys.GetEnumerator();
            while (keys.MoveNext())
            {
                _values[keys.Current] = 0f;
            }
        }

        private void UpdateTrack(int track, double time)
        {
            _valuesUpdated = false;

            if (track >= _lastEventOfTrack.Count ||
                track >= _midiFile.Events[track].Count) return;

            var events = _midiFile.Events[track];
            var lastEvent = _lastEventOfTrack[track];
            if (lastEvent + 1 >= events.Count) return;

            var timeInTicks = (long) (time * 4 * (double) _deltaTicksPerQuarterNote);
            var nextEvent = lastEvent + 1;
            while (nextEvent < events.Count
                   && timeInTicks >= events[nextEvent].AbsoluteTime)
            {
                if (_printLogMessages)
                {
                    Log.Debug(ToMBT(events[nextEvent].AbsoluteTime,
                                    _deltaTicksPerQuarterNote, _timeSignature));
                }

                if (events[nextEvent] is NoteOnEvent noteOnEvent)
                {
                    var channel = noteOnEvent.Channel;
                    var name = noteOnEvent.NoteName;
                    var value = (float)noteOnEvent.Velocity / 127f;
                    var key = $"/channel{channel}/{name}";
                    _values[key] = value;
                    _valuesUpdated = true;

                    if (_printLogMessages)
                        Log.Debug(key + "=" + value);
                }
                else if (events[nextEvent] is NoteEvent noteEvent)
                {
                    var channel = noteEvent.Channel;
                    var name = noteEvent.NoteName;
                    var value = 0.0f;
                    var key = $"/channel{channel}/{name}";
                    _values[key] = value;
                    _valuesUpdated = true;

                    if (_printLogMessages)
                        Log.Debug(key + "=" + value);
                }
                else if (events[nextEvent] is ControlChangeEvent controlChangeEvent)
                {
                    var channel = controlChangeEvent.Channel;
                    var controller = (int) controlChangeEvent.Controller;
                    var value = (float)controlChangeEvent.ControllerValue / 127f;
                    var key = $"/channel{channel}/controller{controller}";
                    _values[key] = value;
                    _valuesUpdated = true;

                    if (_printLogMessages)
                        Log.Debug(key + "=" + value);
                }
                lastEvent = nextEvent;
                nextEvent = lastEvent + 1;
            }
            _lastEventOfTrack[track] = lastEvent;
        }

        // from https://github.com/naudio/NAudio/blob/master/Docs/MidiFile.md
        private static string ToMBT(long eventTime, int ticksPerQuarterNote, TimeSignatureEvent timeSignature)
        {
            var beatsPerBar = timeSignature == null ? 4 : timeSignature.Numerator;
            var ticksPerBar = timeSignature == null ? ticksPerQuarterNote * 4 : (timeSignature.Numerator * ticksPerQuarterNote * 4) / (1 << timeSignature.Denominator);
            var ticksPerBeat = ticksPerBar / beatsPerBar;
            var bar = (eventTime / ticksPerBar);
            var beat = ((eventTime % ticksPerBar) / ticksPerBeat);
            var tick = eventTime % ticksPerBeat;
            return string.Format($"{bar}:{beat}:{tick}");
        }

        // the MIDI file input
        private bool _initialized = false;
        private MF _midiFile = null;
        private MidiEventCollection _midiEventCollection = null;
        private int _deltaTicksPerQuarterNote = 500000 / 60;

        // output data
        private readonly Dict<float> _values;

        // parsing the file
        private TimeSignatureEvent _timeSignature = null;
        private double _lastTimeInBars = 0f;
        private List<int> _lastEventOfTrack = null;
        private bool _printLogMessages = false;
        private bool _valuesUpdated = false;
    }
}