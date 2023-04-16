using System;
using System.Collections.Generic;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using NAudio.Midi;
using Operators.Utils;
using SharpDX;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5
{
    public class MidiInput : Instance<MidiInput>, IDisposable, MidiInConnectionManager.IMidiConsumer
    {
        #region outputs
        [Output(Guid = "01706780-D25B-4C30-A741-8B7B81E04D82", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();
        
        [Output(Guid = "D7114289-4B1D-47E9-B5C1-DCDC8A371087", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> Range = new();
        #endregion
        

        [Input(Guid = "AAD1E576-F144-423F-83B5-5694B1119C23")]
        public readonly InputSlot<Vector2> OutputRange = new();

        [Input(Guid = "4636D6CF-8233-4281-8840-5BA079B5F1A6")]
        public readonly InputSlot<float> DefaultOutputValue = new();
        
        [Input(Guid = "CA3CE08D-6A19-4AD5-9435-08B050753311")]
        public readonly InputSlot<float> Damping = new();
        

        [Input(Guid = "7C681EE6-D071-4284-8585-1C3E03A089EA")]
        public readonly InputSlot<bool> TeachTrigger = new();

        [Input(Guid = "23C34F4C-4BA3-4834-8D51-3E3909751F84")]
        public readonly InputSlot<string> Device = new();

        [Input(Guid = "9B0D32DE-C53C-4DF6-8B29-5E68A5A9C5F9")]
        public readonly InputSlot<int> Channel = new();

        [Input(Guid = "DF81B7B3-F39E-4E5D-8B97-F29DD576A76D")]
        public readonly InputSlot<int> Control = new();

        [Input(Guid = "F650985F-00A7-452A-B3E4-69A8E9A78C3F")]
        public readonly InputSlot<Size2> ControlRange = new();

        [Input(Guid = "6C15E743-9A70-47E7-A0A4-75636817E441")]
        public readonly InputSlot<bool> PrintLogMessages = new();

        public MidiInput()
        {
            Result.UpdateAction = Update;
            Range.UpdateAction = Update;
            MidiInConnectionManager.RegisterConsumer(this);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;

            MidiInConnectionManager.UnregisterConsumer(this);
        }

        private void Update(EvaluationContext context)
        {
            _trainedDeviceName = Device.GetValue(context);
            _trainedChannel = Channel.GetValue(context);
            _trainedControllerId = Control.GetValue(context);

            _controlRange = ControlRange.GetValue(context);

            _printLogMessages = PrintLogMessages.GetValue(context);

            var teachTrigger = !TeachTrigger.GetValue(context);
            var teachJustTriggered = !teachTrigger && !_oldTeachTrigger;
            _oldTeachTrigger = !teachTrigger;
            var controlRangeSize = (_controlRange.Height - _controlRange.Width).Clamp(1, 128);

            if (_valuesForControlRange == null || _valuesForControlRange.Count != controlRangeSize)
            {
                _valuesForControlRange = new List<float>(controlRangeSize);
                _valuesForControlRange.AddRange(new float[controlRangeSize]); //initialize list values
            }

            if (teachJustTriggered)
            {
                MidiInConnectionManager.Rescan();
                _teachingActive = true;
                _lastMatchingSignals.Clear();
                _currentControllerValue = 0;
            }

            lock (this)
            {
                
                foreach (var signal in _lastMatchingSignals)
                {
                    if (_teachingActive)
                    {
                        Device.TypedInputValue.Value = _lastMessageDevice.ProductName;
                        Device.Input.IsDefault = false;
                        Device.DirtyFlag.Invalidate();

                        Channel.TypedInputValue.Value = signal.Channel;
                        Channel.Input.IsDefault = false;
                        Channel.DirtyFlag.Invalidate();

                        Control.TypedInputValue.Value = signal.ControllerId;
                        Control.Input.IsDefault = false;
                        Control.DirtyFlag.Invalidate();

                        TeachTrigger.TypedInputValue.Value = false;
                        TeachTrigger.Input.IsDefault = false;
                        TeachTrigger.DirtyFlag.Invalidate();

                        _trainedDeviceName = _lastMessageDevice.ProductName;
                        _trainedChannel = signal.Channel;
                        _trainedControllerId = signal.ControllerId;
                        _teachingActive = false;

                        TeachTrigger.TypedInputValue.Value = false;
                    }

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

                    LastMessageTime = Playback.RunTimeInSecs;
                    _isDefaultValue = false;
                }

                _lastMatchingSignals.Clear();
            }

            if (_isDefaultValue)
            {
                Result.Value = DefaultOutputValue.GetValue(context);
                return;
            }

            var outRange = OutputRange.GetValue(context);
            var currentValue = UseControlRange
                                          ? _currentControllerId
                                          : MathUtils.RemapAndClamp(_currentControllerValue, 0, 127, outRange.X, outRange.Y);
            
            _dampedOutputValue = MathUtils.Lerp(currentValue,_dampedOutputValue,  Damping.GetValue(context));
            if (!float.IsNormal(_dampedOutputValue))
                _dampedOutputValue = 0;
            
            Result.Value = _dampedOutputValue;
            Range.Value = _valuesForControlRange;
        }

        public void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg)
        {
        }

        public void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg)
        {
            lock (this)
            {
                if (!(sender is MidiIn midiIn) || msg.MidiEvent == null)
                    return;
                
                MidiSignal newSignal = null;

                var device = MidiInConnectionManager.GetDescriptionForMidiIn(midiIn);

                if (msg.MidiEvent is ControlChangeEvent controlEvent)
                {
                    if (_printLogMessages)
                        Log.Debug("" + controlEvent + "  ControlValue :" + controlEvent.ControllerValue, this);

                    if (!UseControlRange)
                    {
                        newSignal = new MidiSignal()
                                        {
                                            Channel = controlEvent.Channel,
                                            ControllerId = (int)controlEvent.Controller,
                                            ControllerValue = (int)controlEvent.ControllerValue,
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
                                Log.Debug("" + noteEvent + "  ControlValue :" + noteEvent.NoteNumber, this);

                            newSignal = new MidiSignal()
                                            {
                                                Channel = noteEvent.Channel,
                                                ControllerId = noteEvent.NoteNumber,
                                                ControllerValue = noteEvent.Velocity,
                                            };
                            break;
                        }
                        case MidiCommandCode.NoteOff:
                            newSignal = new MidiSignal()
                                            {
                                                Channel = noteEvent.Channel,
                                                ControllerId = noteEvent.NoteNumber,
                                                ControllerValue = 0,
                                            };
                            break;
                    }
                }
                else if (msg.MidiEvent.CommandCode == MidiCommandCode.TimingClock
                         && device.ProductName == _trainedDeviceName)
                {
                    //_timingMsgCount++;
                }

                if (newSignal != null)
                {
                    var matchesDevice = String.IsNullOrEmpty(_trainedDeviceName) || device.ProductName == _trainedDeviceName;
                    var matchesChannel = _trainedChannel < 0 || newSignal.Channel == _trainedChannel;

                    var matchesSingleController = _trainedControllerId < 0 || newSignal.ControllerId == _trainedControllerId;
                    var matchesControlRange = _trainedControllerId < 0 ||
                                              (newSignal.ControllerId >= _controlRange.Width && newSignal.ControllerId <= _controlRange.Height);

                    var matchesController = UseControlRange
                                                ? matchesControlRange
                                                : matchesSingleController;

                    if (_teachingActive || (matchesDevice && matchesChannel && matchesController))
                    {
                        _lastMatchingSignals.Add(newSignal);
                        ;
                        _lastMessageDevice = device;
                        _isDefaultValue = false;
                    }
                }
            }
        }

        private Size2 _controlRange;
        private bool UseControlRange => _controlRange.Width > 0 || _controlRange.Height > 0;
        private List<float> _valuesForControlRange;

        private static readonly List<MidiInput> Instances = new List<MidiInput>();

        private class MidiSignal
        {
            public int Channel;
            public int ControllerId;
            public int ControllerValue;
        }

        #region implement IMidiInput ------------------------------
        public string GetDevice()
        {
            return _trainedDeviceName;
        }

        public float GetChannel()
        {
            return _trainedChannel;
        }

        public float GetControl()
        {
            return _trainedControllerId;
        }

        public float CurrentMidiValue
        {
            get { return _currentControllerValue; }
            set
            {
                _currentControllerValue = value;
                _isDefaultValue = false;
            }
        }

        #endregion

        public double LastMessageTime;
        
        private bool _printLogMessages;
        private bool _isDefaultValue = true;
        private bool _oldTeachTrigger;
        private bool _teachingActive;
        private string _trainedDeviceName;
        private int _trainedChannel = -1;
        private int _trainedControllerId = -1;
        private readonly List<MidiSignal> _lastMatchingSignals = new(10);
        private MidiInCapabilities _lastMessageDevice;

        private float _currentControllerValue;
        private float _dampedOutputValue;
        private int _currentControllerId;
    }
}