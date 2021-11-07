using System;
using System.Collections.Generic;
using System.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using NAudio.Midi;
using SharpDX;
using T3.Core;
using T3.Core.Logging;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5
{
    
    public static class MidiInConnectionManager
    {
        public static void RegisterConsumer(IMidiConsumer consumer)
        {
            CloseMidiDevices();
            MidiConsumers.Add(consumer);
            ScanAndRegisterToMidiDevices();
        }
        
        public static void UnregisterConsumer(IMidiConsumer consumer)
        {
            if (!MidiConsumers.Contains(consumer))
                return;

            foreach (var midiIn in MidiInsWithDevices.Keys)
            {
                midiIn.MessageReceived -= consumer.MessageReceivedHandler;
                midiIn.ErrorReceived -= consumer.ErrorReceivedHandler;
            }

            MidiConsumers.Remove(consumer);
            if (MidiConsumers.Count == 0)
            {
                CloseMidiDevices();
            }
        }

        public static void Rescan()
        {
            CloseMidiDevices();
            ScanAndRegisterToMidiDevices(logInformation: true);
        }
        
        
        public interface IMidiConsumer
        {
            void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg);
            void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg);
        }
        
        public static MidiInCapabilities GetDescriptionForMidiIn(MidiIn midiIn)
        {
            MidiInsWithDevices.TryGetValue(midiIn,  out  var description);
            return description;
        }

        public static MidiIn GetMidiInForProductNameHash(int hash)
        {
            MidiInsByDeviceIdHash.TryGetValue(hash, out var midiIn);
            return midiIn;
        }

        
        private static void ScanAndRegisterToMidiDevices(bool logInformation = false)
        {
            for (int index = 0; index < MidiIn.NumberOfDevices; index++)
            {
                var deviceInfo = MidiIn.DeviceInfo(index);

                if (logInformation)
                    Log.Debug("Scanning " + deviceInfo.ProductName);

                MidiIn newMidiIn;
                try
                {
                    newMidiIn = new MidiIn(index);
                }
                catch (NAudio.MmException e)
                {
                    Log.Error(" > " + e.Message + " " + MidiIn.DeviceInfo(index).ProductName);
                    continue;
                }

                foreach (var midiConsumer in MidiConsumers)
                {
                    newMidiIn.MessageReceived += midiConsumer.MessageReceivedHandler;
                    newMidiIn.ErrorReceived += midiConsumer.ErrorReceivedHandler;
                }

                newMidiIn.Start();
                MidiInsWithDevices[newMidiIn] = deviceInfo;
                MidiInsByDeviceIdHash[deviceInfo.ProductName.GetHashCode()] = newMidiIn;
            }
        }

        private static void CloseMidiDevices()
        {
            foreach (var midiInputDevice in MidiInsWithDevices.Keys)
            {
                foreach (var midiConsumer in MidiConsumers)
                {
                    midiInputDevice.MessageReceived -= midiConsumer.MessageReceivedHandler;
                    midiInputDevice.ErrorReceived -= midiConsumer.ErrorReceivedHandler;
                }

                try
                {
                    midiInputDevice.Stop();
                    midiInputDevice.Close();
                    midiInputDevice.Dispose();
                }
                catch (Exception e)
                {
                    Log.Debug("exception: " + e);
                }
            }

            MidiInsWithDevices.Clear();
            MidiInsByDeviceIdHash.Clear();
        }
        
        private static readonly List<IMidiConsumer> MidiConsumers = new List<IMidiConsumer>();
        private static readonly Dictionary<MidiIn, MidiInCapabilities> MidiInsWithDevices = new Dictionary<MidiIn, MidiInCapabilities>();
        private static readonly Dictionary<int, MidiIn> MidiInsByDeviceIdHash = new Dictionary<int, MidiIn>();
    }
    
    
    

    public class MidiInput : Instance<MidiInput>, IDisposable, MidiInConnectionManager.IMidiConsumer
    {
        [Output(Guid = "01706780-D25B-4C30-A741-8B7B81E04D82", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new Slot<float>();

        [Output(Guid = "D7114289-4B1D-47E9-B5C1-DCDC8A371087", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> Range = new Slot<List<float>>();

        [Input(Guid = "AAD1E576-F144-423F-83B5-5694B1119C23")]
        public readonly InputSlot<Vector2> OutputRange = new InputSlot<Vector2>();

        [Input(Guid = "4636D6CF-8233-4281-8840-5BA079B5F1A6")]
        public readonly InputSlot<float> DefaultMidiValue = new InputSlot<float>();

        [Input(Guid = "3B350FF2-004C-457B-983D-21D11A01D170")]
        public readonly InputSlot<bool> AllowPresets = new InputSlot<bool>();

        [Input(Guid = "7C681EE6-D071-4284-8585-1C3E03A089EA")]
        public readonly InputSlot<bool> TeachTrigger = new InputSlot<bool>();

        [Input(Guid = "23C34F4C-4BA3-4834-8D51-3E3909751F84")]
        public readonly InputSlot<string> Device = new InputSlot<string>();

        [Input(Guid = "9B0D32DE-C53C-4DF6-8B29-5E68A5A9C5F9")]
        public readonly InputSlot<int> Channel = new InputSlot<int>();

        [Input(Guid = "DF81B7B3-F39E-4E5D-8B97-F29DD576A76D")]
        public readonly InputSlot<int> Control = new InputSlot<int>();

        [Input(Guid = "F650985F-00A7-452A-B3E4-69A8E9A78C3F")]
        public readonly InputSlot<Size2> ControlRange = new InputSlot<Size2>();

        [Input(Guid = "6C15E743-9A70-47E7-A0A4-75636817E441")]
        public readonly InputSlot<bool> PrintLogMessages = new InputSlot<bool>();

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
            _allowPresets = AllowPresets.GetValue(context);

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

                    _isDefaultValue = false;
                }

                _lastMatchingSignals.Clear();
            }

            if (_isDefaultValue)
            {
                Result.Value = DefaultMidiValue.GetValue(context);
                return;
            }

            var outRange = OutputRange.GetValue(context);
            Result.Value = UseControlRange
                               ? _currentControllerId
                               : MathUtils.RemapAndClamp(_currentControllerValue, 0, 127, outRange.X, outRange.Y);
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
                        Log.Debug("" + controlEvent + "  ControlValue :" + controlEvent.ControllerValue);

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
                                Log.Debug("" + noteEvent + "  ControlValue :" + noteEvent.NoteNumber);

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
        //private static readonly Dictionary<MidiIn, MidiInCapabilities> MidiInsWithDevices = new Dictionary<MidiIn, MidiInCapabilities>();

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
                //Changed = true;
                //_valueHasBeenChanged = true;
                //Log.Debug(this, "Setting value to :" + value);
                //_waitingForPickup = false;
            }
        }

        public float TargetMidiValue { get { return _currentControllerValue; } set { CurrentMidiValue = value; } }

        private bool _allowPresets;
        #endregion

        private bool _printLogMessages;
        private bool _isDefaultValue = true;
        private bool _oldTeachTrigger;
        private bool _teachingActive;
        private string _trainedDeviceName;
        private int _trainedChannel = -1;
        private int _trainedControllerId = -1;
        private readonly List<MidiSignal> _lastMatchingSignals = new List<MidiSignal>(10);
        private MidiInCapabilities _lastMessageDevice;

        private float _currentControllerValue;
        private int _currentControllerId;

        // private float _previousValue;
    }
}