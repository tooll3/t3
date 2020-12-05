using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using T3.Core.Logging;
using T3.Gui.Interaction.PresetControl.InputCommands;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

namespace T3.Gui.Interaction.PresetControl.Midi
{
    public abstract class MidiDevice : IControllerInputDevice, MidiInConnectionManager.IMidiConsumer
    {
        public MidiDevice()
        {
            MidiInConnectionManager.RegisterConsumer(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Mental notes / further ideas
        /// - adjust display to indicate valid key combinations, e.g. highlight preset buttons valid for deletion
        /// - enforce an order for valid commands (e.g. CUE+P01+P02  CUE+P02+01) 
        /// </remarks>
        public virtual void Update(PresetSystem manager, MidiIn midiIn)
        {
            ProcessSignals();

            var isAnyButtonPressed = _signalsForNextCommand.Values.Any(signal => signal.IsPressed);

            if (!_buttonCombinationStarted || isAnyButtonPressed)
                return;

            if (CommandTriggerCombinations != null)
            {
                foreach (var ctc in CommandTriggerCombinations)
                {
                    var command = ctc.MatchesCommandPresses(_signalsForNextCommand.Values.ToList());
                    if (command == null)
                        continue;

                    if (command.IsInstant)
                        command.ExecuteOnce(manager);
                }
            }

            _signalsForNextCommand.Clear();

            _buttonCombinationStarted = false;
        }

        public abstract int GetProductNameHash();

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

    public class ApcMiniDevice : MidiDevice
    {
        private static int _currentPresetIndex = 0;
        private static int _pageIndex = 0;

        public override void Update(PresetSystem manager, MidiIn midiIn)
        {
            base.Update(manager, midiIn);
            SendStatusToApc();
        }

        private void SendStatusToApc()
        {
            //var midiOut = ApcMiniController.GetConnectedController();
            var midiOut = MidiOutConnectionManager.GetConnectedController(_productNameHash);
            if (midiOut == null)
                return;

            UpdatePresetLeds(midiOut);
            UpdatePageLeds(midiOut);
        }

        private const int PRESET_ROWS = 7;
        private const int PRESET_COLUMNS = 8;
        private const int PAGE_PRESET_COUNT = PRESET_COLUMNS * PRESET_ROWS;

        private readonly int _productNameHash = "APC MINI".GetHashCode();

        public override int GetProductNameHash()
        {
            return _productNameHash;
        }

        private void UpdatePageLeds(MidiOut midiOut)
        {
            for (var i = 0; i < PRESET_COLUMNS; i++)
            {
                var isActivePresetInPage = i == _currentPresetIndex / PAGE_PRESET_COUNT;
                var colorForInactivePage =
                    isActivePresetInPage ? ApcButtonColor.RedBlinking : ApcButtonColor.Off;

                var colorForActivePage = i == _pageIndex ? ApcButtonColor.Red : colorForInactivePage;
                SendColor(midiOut, i, colorForActivePage);
            }
        }

        private void UpdatePresetLeds(MidiOut midiOut)
        {
            var pageOffset = _pageIndex * PAGE_PRESET_COUNT;

            for (var index = 0; index < PAGE_PRESET_COUNT; index++)
            {
                var apcButtonRow = index / PRESET_COLUMNS + 1;
                var apcButtonColumn = index % PRESET_COLUMNS;
                var apcButtonIndex = apcButtonRow * PRESET_COLUMNS + apcButtonColumn;

                var presetIndex = index + pageOffset;
                var isCurrentIndex = presetIndex == _currentPresetIndex;
                //var p = TryLoadingPreset(presetIndex);

                //                    Log.Debug("isCurrent " + presetIndex + " ==" + _currentPresetIndex + " " + isCurrentIndex);

                // var isValid = p != null;
                // if (isValid)
                // {
                var colorForComplete = true ? ApcButtonColor.Green : ApcButtonColor.Yellow;
                var colorForActive = isCurrentIndex ? ApcButtonColor.Red : colorForComplete;
                //
                SendColor(midiOut, apcButtonIndex, colorForActive);
                // }
                // else
                // {
                //     var colorForEmpty = isCurrentIndex ? ApcButtonColor.RedBlinking : ApcButtonColor.Off;
                //     SendColor(midiOut, apcButtonIndex, colorForEmpty);
                // }
            }
        }

        private static void SendColor(MidiOut midiOut, int apcControlIndex, ApcButtonColor colorCode)
        {
            midiOut.Send(MidiMessage.StartNote(apcControlIndex, (int)colorCode, 1).RawData);
            midiOut.Send(MidiMessage.StopNote(apcControlIndex, 0, 1).RawData);
        }

        private enum ApcButtonColor
        {
            Off,
            Green,
            GreenBlinking,
            Red,
            RedBlinking,
            Yellow,
            YellowBlinking,
        }
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

        public override int GetProductNameHash()
        {
            return _productNameHash;
        }

        private readonly int _productNameHash = "Korg NanoControl".GetHashCode(); //Todo: this needs the correct product name
    }
}