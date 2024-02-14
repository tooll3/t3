using System;
using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using NAudio.Midi;
using Operators.Utils;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator.Interfaces;
using T3.Core.Utils;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5
{
    public class MidiInput : Instance<MidiInput>, MidiInConnectionManager.IMidiConsumer, IStatusProvider
    {
        [Output(Guid = "01706780-D25B-4C30-A741-8B7B81E04D82")]
        public readonly Slot<float> Result = new();

        [Output(Guid = "D7114289-4B1D-47E9-B5C1-DCDC8A371087")]
        public readonly Slot<List<float>> Range = new();
        
        [Output(Guid = "4BF74648-207F-4275-83BA-09E1C048C33B")]
        public readonly Slot<bool> WasHit = new();
        
        public MidiInput()
        {
            Result.UpdateAction = Update;
            Range.UpdateAction = Update;
            WasHit.UpdateAction = Update;
        }

        private bool _initialized;
        
        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            MidiInConnectionManager.UnregisterConsumer(this);
        }

        private void Update(EvaluationContext context)
        {
            if (!_initialized)
            {
                MidiInConnectionManager.RegisterConsumer(this);
                _initialized = true;
            }
            
            _trainedDeviceName = Device.GetValue(context);

            var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(_trainedDeviceName.GetHashCode());
            _warningMessage = midiIn == null ? $"Midi device '{_trainedDeviceName}' is not captured.\nYou can try Windows » Settings » Midi » Rescan Devices." : null;
            
            _trainedChannel = Channel.GetValue(context);
            _trainedControllerId = Control.GetValue(context);
            _trainedEventType = EventType.GetEnumValue<MidiEventTypes>(context);

            var defaultOutputValue = DefaultOutputValue.GetValue(context);
            var damping = Damping.GetValue(context);
            var outRange = OutputRange.GetValue(context);

            _controlRange = ControlRange.GetValue(context);

            _printLogMessages = PrintLogMessages.GetValue(context);

            var controlRangeSize = (_controlRange.Height - _controlRange.Width).Clamp(1, 128);

            if (_valuesForControlRange == null || _valuesForControlRange.Count != controlRangeSize)
            {
                _valuesForControlRange = new List<float>(controlRangeSize);
                _valuesForControlRange.AddRange(new float[controlRangeSize]); //initialize list values
            }

            if (MathUtils.WasTriggered(TeachTrigger.GetValue(context), ref _oldTeachTrigger))
            {
                //MidiInConnectionManager.Rescan();
                _teachingActive = true;
                _lastMatchingSignals.Clear();
                _currentControllerValue = 0;
            }

            var wasHit = false;
            lock (this)
            {
                foreach (var signal in _lastMatchingSignals)
                {
                    if (_teachingActive)
                    {
                        // The teaching mode shouldn't override the connected nodes
                        if (!Device.IsConnected) {
                            Device.SetTypedInputValue(_lastMessageDevice.ProductName);
                            _trainedDeviceName = _lastMessageDevice.ProductName;
                        }

                        if (!Channel.IsConnected) {
                            Channel.SetTypedInputValue(signal.Channel);
                            _trainedChannel = signal.Channel;
                        }

                        if (!Control.IsConnected) {
                            Control.SetTypedInputValue(signal.ControllerId);
                            _trainedControllerId = signal.ControllerId;
                        }

                        if (!EventType.IsConnected) {
                            EventType.SetTypedInputValue((int)signal.EventType);
                            _trainedEventType = signal.EventType;
                        }

                        TeachTrigger.SetTypedInputValue(false);
                        _teachingActive = false;
                    }

                    var hasValueChanged = Math.Abs(_currentControllerValue - signal.ControllerValue) > 0.001f;
                    _currentControllerValue = signal.ControllerValue;
                    _currentControllerId = signal.ControllerId;

                    var isWithinControlRange =
                        signal.ControllerId >= _controlRange.Width &&
                        signal.ControllerId < _controlRange.Height;

                    if (isWithinControlRange)
                    {
                        var index = signal.ControllerId - _controlRange.Width;
                        _valuesForControlRange[index] = signal.ControllerValue;
                    }

                    if (hasValueChanged && signal.ControllerValue > 0 && !Control.IsConnected)
                    {
                        Control.Value = _currentControllerId;
                        wasHit = true;
                    }
                    
                    LastMessageTime = Playback.RunTimeInSecs;
                    _isDefaultValue = false;
                }

                _lastMatchingSignals.Clear();
            }

            if (_isDefaultValue && _trainedEventType != MidiEventTypes.MidiTime)
            {
                Result.Value = defaultOutputValue;
                Result.DirtyFlag.Clear();
                Range.DirtyFlag.Clear();
                WasHit.DirtyFlag.Clear();
                return;
            }

            var currentValue = UseControlRange
                                   ? _currentControllerId
                                   : MathUtils.RemapAndClamp(_currentControllerValue, 0, 127, outRange.X, outRange.Y);

            if (_trainedEventType == MidiEventTypes.MidiTime)
            {
                currentValue = (float)(_timingMsgCount / (24.0 * 4 ));
            }
            

            _dampedOutputValue = MathUtils.Lerp(currentValue, _dampedOutputValue, damping);

            var reachTarget = MathF.Abs(_dampedOutputValue - currentValue) < 0.0001f;
            var needsUpdateNextFrame = !reachTarget || wasHit;
            Result.DirtyFlag.Trigger = needsUpdateNextFrame ? DirtyFlagTrigger.Animated : DirtyFlagTrigger.None;
            Range.DirtyFlag.Trigger =  needsUpdateNextFrame ? DirtyFlagTrigger.Animated : DirtyFlagTrigger.None;
            WasHit.DirtyFlag.Trigger = needsUpdateNextFrame ? DirtyFlagTrigger.Animated : DirtyFlagTrigger.None;

            if (reachTarget)
            {
                _dampedOutputValue = currentValue;
            }

            if (!float.IsNormal(_dampedOutputValue))
                _dampedOutputValue = 0;

            WasHit.Value = wasHit;
            Result.Value = _dampedOutputValue;
            Range.Value = _valuesForControlRange;
            
            Result.DirtyFlag.Clear();
            Range.DirtyFlag.Clear();
            WasHit.DirtyFlag.Clear();
        }

        public void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg)
        {
        }

        /// <summary>
        /// This will cause update to be called on next frame 
        /// </summary>
        private void FlagAsDirty()
        {
            Result.DirtyFlag.Invalidate();
            Range.DirtyFlag.Invalidate();
            WasHit.DirtyFlag.Invalidate();
        }

        /// <remarks>
        /// This comes in multi threaded
        /// </remarks>
        public void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg)
        {
            lock (this)
            {
                if (sender is not MidiIn midiIn || msg.MidiEvent == null)
                    return;

                MidiSignal newSignal = null;

                var device = MidiInConnectionManager.GetDescriptionForMidiIn(midiIn);


                if (msg.MidiEvent is ControlChangeEvent controlEvent)
                {
                    if (_printLogMessages)
                        Log.Debug($"{device}/{controlEvent}  ControlValue :{controlEvent.ControllerValue}", this);

                    if (!UseControlRange)
                    {
                        newSignal = new MidiSignal()
                                        {
                                            Channel = controlEvent.Channel,
                                            ControllerId = (int)controlEvent.Controller,
                                            ControllerValue = controlEvent.ControllerValue,
                                            EventType = MidiEventTypes.ControllerChanges,
                                        };
                    }
                }
                
                if (msg.MidiEvent is NoteEvent noteEvent)
                {
                    switch (noteEvent.CommandCode)
                    {
                        case MidiCommandCode.NoteOn:
                        {
                            if (_printLogMessages)
                                Log.Debug($"{device}/{noteEvent}  ControlValue :{noteEvent.NoteNumber}", this);

                            newSignal = new MidiSignal()
                                            {
                                                Channel = noteEvent.Channel,
                                                ControllerId = noteEvent.NoteNumber,
                                                ControllerValue = noteEvent.Velocity,
                                                EventType = MidiEventTypes.Notes,
                                            };
                            break;
                        }
                        case MidiCommandCode.NoteOff:
                            newSignal = new MidiSignal()
                                            {
                                                Channel = noteEvent.Channel,
                                                ControllerId = noteEvent.NoteNumber,
                                                ControllerValue = 0,
                                                EventType = MidiEventTypes.Notes,
                                            };
                            break;

                    }
                }
                
                if (!_teachingActive && msg.MidiEvent.CommandCode == MidiCommandCode.TimingClock)
                {
                    _timingMsgCount++;
                    if (_trainedEventType == MidiEventTypes.MidiTime)
                    {
                        newSignal = new MidiSignal()
                                        {
                                            Channel = msg.MidiEvent.Channel,
                                            ControllerId = 0,
                                            ControllerValue = _timingMsgCount,
                                            EventType = MidiEventTypes.MidiTime,
                                        };                    
                    }                    
                }

                if (newSignal == null)
                    return;

                var matchesDevice = string.IsNullOrEmpty(_trainedDeviceName) || device.ProductName == _trainedDeviceName;
                var matchesChannel = _trainedChannel < 0 || newSignal.Channel == _trainedChannel;

                var matchesSingleController = _trainedControllerId < 0 || newSignal.ControllerId == _trainedControllerId;
                var matchesControlRange = _trainedControllerId < 0 ||
                                          (newSignal.ControllerId >= _controlRange.Width && newSignal.ControllerId <= _controlRange.Height);

                var matchesController = UseControlRange
                                            ? matchesControlRange
                                            : matchesSingleController;

                var matchesEventType = _trainedEventType == MidiEventTypes.All || _trainedEventType == newSignal.EventType;

                if (_teachingActive || (matchesDevice && matchesChannel && matchesController && matchesEventType))
                {
                    _lastMatchingSignals.Add(newSignal);
                    _lastMessageDevice = device;
                    _isDefaultValue = false;
                    FlagAsDirty();
                }
            }
        }

        void MidiInConnectionManager.IMidiConsumer.OnSettingsChanged()
        {
            Result.DirtyFlag.Invalidate();
            Range.DirtyFlag.Invalidate();
            WasHit.DirtyFlag.Invalidate();
        }
        
        public IStatusProvider.StatusLevel GetStatusLevel()
        {
            return string.IsNullOrEmpty(_warningMessage) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Error;
        }

        public string GetStatusMessage()
        {
            return _warningMessage;
        }

        private string _warningMessage;
        

        private Int2 _controlRange;
        private bool UseControlRange => _controlRange.Width > 0 || _controlRange.Height > 0;
        private List<float> _valuesForControlRange;

        private class MidiSignal
        {
            public int Channel;
            public int ControllerId;
            public int ControllerValue;
            public MidiEventTypes EventType;
        }

        public double LastMessageTime;

        private bool _printLogMessages;
        private bool _isDefaultValue = true;
        private bool _oldTeachTrigger;
        private bool _teachingActive;
        private string _trainedDeviceName;
        private int _trainedChannel = -1;
        private int _trainedControllerId = -1;
        private MidiEventTypes _trainedEventType;
        private readonly List<MidiSignal> _lastMatchingSignals = new(10);
        private MidiInCapabilities _lastMessageDevice;

        private float _currentControllerValue;
        private float _dampedOutputValue;
        private int _currentControllerId;
        private int _timingMsgCount;

        private enum MidiEventTypes
        {
            All,
            Notes,
            ControllerChanges,
            MidiTime,
        }
        
        [Input(Guid = "AAD1E576-F144-423F-83B5-5694B1119C23")]
        public readonly InputSlot<Vector2> OutputRange = new();

        [Input(Guid = "4636D6CF-8233-4281-8840-5BA079B5F1A6")]
        public readonly InputSlot<float> DefaultOutputValue = new();

        [Input(Guid = "CA3CE08D-6A19-4AD5-9435-08B050753311")]
        public readonly InputSlot<float> Damping = new();

        [Input(Guid = "7C681EE6-D071-4284-8585-1C3E03A089EA")]
        public readonly InputSlot<bool> TeachTrigger = new();
        
        [Input(Guid = "044168EB-791C-405F-867F-3D5702924165", MappedType = typeof(MidiEventTypes))]
        public readonly InputSlot<int> EventType = new();

        [Input(Guid = "23C34F4C-4BA3-4834-8D51-3E3909751F84")]
        public readonly InputSlot<string> Device = new();


        [Input(Guid = "9B0D32DE-C53C-4DF6-8B29-5E68A5A9C5F9")]
        public readonly InputSlot<int> Channel = new();

        [Input(Guid = "DF81B7B3-F39E-4E5D-8B97-F29DD576A76D")]
        public readonly InputSlot<int> Control = new();

        [Input(Guid = "F650985F-00A7-452A-B3E4-69A8E9A78C3F")]
        public readonly InputSlot<Int2> ControlRange = new();

        [Input(Guid = "6C15E743-9A70-47E7-A0A4-75636817E441")]
        public readonly InputSlot<bool> PrintLogMessages = new();


    }
}