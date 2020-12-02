using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using T3.Gui.Interaction.PresetControl.InputCommands;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

namespace T3.Gui.Interaction.PresetControl.Midi
{
    public abstract class MidiDevice : IControllerInputDevice, MidiConnectionManager.IMidiConsumer
    {
        public MidiDevice()
        {
            MidiConnectionManager.RegisterConsumer(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Mental notes / further ideas
        /// - adjust display to indicate valid key combinations, e.g. highlight preset buttons valid for deletion
        /// - enforce an order for valid commands (e.g. CUE+P01+P02  CUE+P02+01) 
        /// </remarks>
        public void Update(PresetSystem manager)
        {
            ProcessSignals();

            var isAnyButtonPressed = _signalsForNextCommand.Values.Any(signal => signal.IsPressed);
            
            if (!_buttonCombinationStarted || isAnyButtonPressed)
                return;
            
            foreach (var ctc in CommandTriggerCombinations)
            {
                var command = ctc.MatchesCommandPresses(_signalsForNextCommand.Values.ToList());
                if (command == null)
                    continue;

                if (command.IsInstant)
                    command.ExecuteOnce(manager);
            }

            _signalsForNextCommand.Clear();

            _buttonCombinationStarted = false;
        }

        private void ProcessSignals()
        {
            lock (_signalsSinceLastUpdate)
            {
                foreach (var signal in _signalsSinceLastUpdate)
                {
                    if (_signalsForNextCommand.TryGetValue(signal.ControllerId, out var existingEvent))
                    {
                        if (signal.IsPressed)
                        {
                            _buttonCombinationStarted = true;
                            existingEvent.PressCount++;
                        }
                        else
                        {
                            existingEvent.IsPressed = false;
                        }
                    }
                    else
                    {
                        signal.PressCount = 1;
                        _signalsForNextCommand[signal.ControllerId] = signal;
                        _buttonCombinationStarted = true;
                    }
                }

                _signalsSinceLastUpdate.Clear();
            }
        }

        public void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg)
        {
            lock (this)
            {
                if (!(sender is MidiIn midiIn) || msg.MidiEvent == null)
                    return;

                //var device = MidiConnectionManager.GetDescriptionForMidiIn(midiIn);
                ButtonSignal newSignal = null;
                switch (msg.MidiEvent.CommandCode)
                {
                    case MidiCommandCode.NoteOff:
                    case MidiCommandCode.NoteOn:

                        if (msg.MidiEvent is NoteEvent noteEvent)
                        {
                            //Log.Debug($"{msg.MidiEvent.CommandCode}   :{noteEvent.NoteNumber}");
                            newSignal = new ButtonSignal()
                                            {
                                                Channel = noteEvent.Channel,
                                                ControllerId = noteEvent.NoteNumber,
                                                ControllerValue = noteEvent.Velocity,
                                                IsPressed = msg.MidiEvent.CommandCode != MidiCommandCode.NoteOff,
                                            };
                        }

                        break;

                    case MidiCommandCode.ControlChange:
                        if (msg.MidiEvent is ControlChangeEvent controlChangeEvent)
                        {
                            //Log.Debug($"ControlChange/{controlChangeEvent.CommandCode}  Controller :{controlChangeEvent.Controller}  #{(int)controlChangeEvent.Controller} Value:{controlChangeEvent.ControllerValue}");
                            newSignal = new ButtonSignal()
                                            {
                                                Channel = controlChangeEvent.Channel,
                                                ControllerId = (int)controlChangeEvent.Controller,
                                                ControllerValue = controlChangeEvent.ControllerValue,
                                                IsPressed = controlChangeEvent.ControllerValue != 0,
                                            };
                        }

                        break;
                }

                if (newSignal != null)
                {
                    _signalsSinceLastUpdate.Add(newSignal);
                }
            }
        }

        public void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg)
        {
            //throw new NotImplementedException();
        }

        protected List<CommandTriggerCombination> CommandTriggerCombinations;

        private readonly Dictionary<int, ButtonSignal> _signalsForNextCommand = new Dictionary<int, ButtonSignal>();
        private readonly List<ButtonSignal> _signalsSinceLastUpdate = new List<ButtonSignal>();
        private bool _buttonCombinationStarted;

        // ====================================================================
        #region signal matching
        public readonly struct ControllerRange
        {
            public ControllerRange(int index)
            {
                _index = index;
                _lastIndex = index;
            }

            public ControllerRange(int index, int lastIndex)
            {
                _index = index;
                _lastIndex = lastIndex;
            }

            public bool IncludesIndex(int index)
            {
                return index >= _index && index <= _lastIndex;
            }

            public bool IsRange => _lastIndex > _index;

            private readonly int _index;
            private readonly int _lastIndex;
        }

        protected class CommandTriggerCombination
        {
            public CommandTriggerCombination(ControllerRange[] keyRanges, Type commandType)
            {
                KeyRanges = keyRanges;
                CommandType = commandType;
            }

            private ControllerRange[] KeyRanges;
            private Type CommandType;

            private static List<int> matchingRangeIndices = new List<int>(10);

            public InputCommand MatchesCommandPresses(List<ButtonSignal> buttonSignals)
            {
                var matchedSignalCount = 0;
                matchingRangeIndices.Clear();

                foreach (var requiredControlRanges in KeyRanges)
                {
                    var foundMatchingSignal = false;
                    foreach (var givenSignal in buttonSignals)
                    {
                        if (!requiredControlRanges.IncludesIndex(givenSignal.ControllerId))
                            continue;

                        matchedSignalCount++;
                        foundMatchingSignal = true;
                        if (requiredControlRanges.IsRange)
                            matchingRangeIndices.Add(givenSignal.ControllerId);
                    }

                    if (!foundMatchingSignal)
                        return null;
                }

                // All button pressed must be account for...
                if (matchedSignalCount != buttonSignals.Count)
                    return null;

                var indices = matchingRangeIndices.ToArray();
                var command = (Activator.CreateInstance(CommandType, indices) as ButtonsPressCommand);
                return command;
            }
        }
        #endregion


        protected class ButtonSignal
        {
            public int ControllerId;
            public float PressTime;
            public float ControllerValue;
            public bool IsPressed;
            public int PressCount;
            public int Channel;
        }
    }

    public class Apc40Device : MidiDevice
    {
    }

    public class NanoControl8 : MidiDevice
    {
        public NanoControl8()
        {
            CommandTriggerCombinations = new List<CommandTriggerCombination>()
                                             {
                                                 new CommandTriggerCombination(new[] { ManagerSet, NanoControlR1To8 }, typeof(SavePresetCommand)),
                                                 new CommandTriggerCombination(new[] { NanoControlR1To8 }, typeof(ApplyPresetCommand)),
                                             };
        }

        private static readonly ControllerRange ControlRewind = new ControllerRange(43);
        private static readonly ControllerRange ControlFastForward = new ControllerRange(44);
        private static readonly ControllerRange ControlStop = new ControllerRange(42);
        private static readonly ControllerRange ControlPlay = new ControllerRange(41);
        private static readonly ControllerRange ControlRecord = new ControllerRange(45);
        private static readonly ControllerRange ControlLoop = new ControllerRange(46);
        private static readonly ControllerRange TextPrevious = new ControllerRange(58);
        private static readonly ControllerRange TrackNext = new ControllerRange(59);
        private static readonly ControllerRange ManagerSet = new ControllerRange(60);
        private static readonly ControllerRange ManagerNext = new ControllerRange(61);
        private static readonly ControllerRange ManagerPrevious = new ControllerRange(62);

        private static readonly ControllerRange NanoControlSlider1To8 = new ControllerRange(0, 0 + 7);
        private static readonly ControllerRange NanoControlKnob1To8 = new ControllerRange(16, 16 + 7);
        private static readonly ControllerRange NanoControlSolo1To8 = new ControllerRange(32, 32 + 7);
        private static readonly ControllerRange NanoControlMute1To8 = new ControllerRange(48, 48 + 7);
        private static readonly ControllerRange NanoControlR1To8 = new ControllerRange(64, 64 + 7);
    }
}