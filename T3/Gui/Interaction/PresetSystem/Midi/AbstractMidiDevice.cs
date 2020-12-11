using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using T3.Gui.Interaction.PresetSystem.InputCommands;
using T3.Gui.Interaction.PresetSystem.Model;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    public abstract class AbstractMidiDevice : IControllerInputDevice, MidiInConnectionManager.IMidiConsumer
    {
        protected AbstractMidiDevice()
        {
            MidiInConnectionManager.RegisterConsumer(this);
        }

        public virtual void Update(PresetSystem presetSystem, MidiIn midiIn, CompositionContext context)
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
                        command.ExecuteOnce(presetSystem);
                }
            }

            _signalsForNextCommand.Clear();

            _buttonCombinationStarted = false;
        }

        public abstract int GetProductNameHash();

        // ------------------------------------------------------------------------------------
        #region SignalProcessing
        private void ProcessSignals()
        {
            lock (_signalsSinceLastUpdate)
            {
                foreach (var signal in _signalsSinceLastUpdate)
                {
                    if (_signalsForNextCommand.TryGetValue(signal.ButtonIndex, out var existingEvent))
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
                        _signalsForNextCommand[signal.ButtonIndex] = signal;
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
                if (msg.MidiEvent == null)
                    return;

                ButtonSignal newSignal = null;
                switch (msg.MidiEvent.CommandCode)
                {
                    case MidiCommandCode.NoteOff:
                    case MidiCommandCode.NoteOn:

                        if (msg.MidiEvent is NoteEvent noteEvent)
                        {
                            newSignal = new ButtonSignal()
                                            {
                                                Channel = noteEvent.Channel,
                                                ButtonIndex = noteEvent.NoteNumber,
                                                ControllerValue = noteEvent.Velocity,
                                                IsPressed = msg.MidiEvent.CommandCode != MidiCommandCode.NoteOff,
                                            };
                        }

                        break;

                    case MidiCommandCode.ControlChange:
                        if (msg.MidiEvent is ControlChangeEvent controlChangeEvent)
                        {
                            newSignal = new ButtonSignal()
                                            {
                                                Channel = controlChangeEvent.Channel,
                                                ButtonIndex = (int)controlChangeEvent.Controller,
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
        #endregion

        //---------------------------------------------------------------------------------
        #region SendColors
        protected delegate int ComputeColorForIndex(int index);

        protected void UpdateRangeLeds(MidiOut midiOut, ButtonRange range, ComputeColorForIndex computeColorForIndex)
        {
            foreach (var buttonIndex in range.Indices())
            {
                var mappedIndex = range.GetMappedIndex(buttonIndex);
                SendColor(midiOut, buttonIndex, computeColorForIndex(mappedIndex));
            }
        }

        private static void SendColor(MidiOut midiOut, int apcControlIndex, int colorCode)
        {
            if (CacheControllerColors[apcControlIndex] == (int)colorCode)
                return;

            const int defaultChannel = 1;
            var noteOnEvent = new NoteOnEvent(0, defaultChannel, apcControlIndex, (int)colorCode, 50);
            midiOut.Send(noteOnEvent.GetAsShortMessage());

            //Previous implementation from T2
            //midiOut.Send(MidiMessage.StartNote(apcControlIndex, (int)colorCode, 1).RawData);
            //midiOut.Send(MidiMessage.StopNote(apcControlIndex, 0, 1).RawData);
            CacheControllerColors[apcControlIndex] = (int)colorCode;
        }

        private static readonly int[] CacheControllerColors = Enumerable.Repeat(-1, 256).ToArray();

        protected List<CommandTriggerCombination> CommandTriggerCombinations;

        private readonly Dictionary<int, ButtonSignal> _signalsForNextCommand = new Dictionary<int, ButtonSignal>();
        private readonly List<ButtonSignal> _signalsSinceLastUpdate = new List<ButtonSignal>();
        private bool _buttonCombinationStarted;
        #endregion
    }
}