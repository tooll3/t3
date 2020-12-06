using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;

using T3.Gui.Interaction.PresetSystem.InputCommands;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

namespace T3.Gui.Interaction.PresetSystem.Midi
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
        public virtual void Update(PresetSystem presetSystem, MidiIn midiIn, PresetConfiguration config)
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
                        command.ExecuteOnce(presetSystem, this);
                }
            }

            _signalsForNextCommand.Clear();

            _buttonCombinationStarted = false;
        }

        public abstract int GetProductNameHash();
        public abstract PresetConfiguration.PresetAddress GetAddressForIndex(int index);

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
                // if (!(sender is MidiIn midiIn) || msg.MidiEvent == null)
                //     return;

                if (msg.MidiEvent == null)
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
 
        #endregion


    }
}