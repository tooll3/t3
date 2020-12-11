using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using T3.Core.Logging;
using T3.Gui.Interaction.PresetSystem.Model;
using T3.Operators.Types.Id_59a0458e_2f3a_4856_96cd_32936f783cc5;

namespace T3.Gui.Interaction.PresetSystem.Midi
{
    public abstract class AbstractMidiDevice : IControllerInputDevice, MidiInConnectionManager.IMidiConsumer
    {
        protected AbstractMidiDevice()
        {
            //if(MidiInConnectionManager.GetMidiInForProductNameHash(GetProductNameHash()) != null)
            MidiInConnectionManager.RegisterConsumer(this);
        }

        [Flags]
        public enum InputModes
        {
            Default = 1 << 1,
            BankNavigation = 1 << 2,
            Delete = 1 << 3,
            Save = 1 << 4,
            Queue = 1 << 5,
            None = 0,
        }

        public InputModes ActiveMode = InputModes.Default;

        public virtual void Update(PresetSystem presetSystem, MidiIn midiIn, CompositionContext context)
        {
            ProcessSignals();

            if (_signalsForButtonCombination.Count == 0)
                return;

            var releasedMode = InputModes.None;

            // Update modes
            foreach (var modeButton in ModeButtons)
            {
                var matchingSignal = _signalsForButtonCombination.Values.SingleOrDefault(s => modeButton.ButtonRange.IncludesButtonIndex(s.ButtonId));
                if (matchingSignal == null)
                    continue;

                if (matchingSignal.State == ButtonSignal.States.JustPressed)
                {
                    if (ActiveMode == InputModes.Default)
                    {
                        ActiveMode = modeButton.Mode;
                    }
                }
                else if (matchingSignal.State == ButtonSignal.States.Released && ActiveMode == modeButton.Mode)
                {
                    releasedMode = modeButton.Mode;
                    ActiveMode = InputModes.Default;
                }
            }

            if (CommandTriggerCombinations == null)
                return;

            var isAnyButtonPressed = _signalsForButtonCombination.Values.Any(signal => (signal.State == ButtonSignal.States.JustPressed
                                                                                        || signal.State == ButtonSignal.States.Hold));

            foreach (var ctc in CommandTriggerCombinations)
            {
                ctc.InvokeMatchingCommands(_signalsForButtonCombination.Values.ToList(), ActiveMode, releasedMode);
            }

            if (!isAnyButtonPressed)
            {
                _signalsForButtonCombination.Clear();
            }
        }

        public abstract int GetProductNameHash();

        protected List<CommandTriggerCombination> CommandTriggerCombinations;
        protected List<ModeButton> ModeButtons;

        // ------------------------------------------------------------------------------------
        #region Process button Signals
        private void ProcessSignals()
        {
            lock (_signalsSinceLastUpdate)
            {
                foreach (var earlierSignal in _signalsForButtonCombination.Values)
                {
                    if (earlierSignal.State == ButtonSignal.States.JustPressed)
                        earlierSignal.State = ButtonSignal.States.Hold;
                }

                foreach (var newSignal in _signalsSinceLastUpdate)
                {
                    if (_signalsForButtonCombination.TryGetValue(newSignal.ButtonId, out var earlierSignal))
                    {
                        earlierSignal.State = newSignal.State;
                    }
                    else
                    {
                        _signalsForButtonCombination[newSignal.ButtonId] = newSignal;
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
                
                switch (msg.MidiEvent.CommandCode)
                {
                    case MidiCommandCode.NoteOff:
                    case MidiCommandCode.NoteOn:
                        if (!(msg.MidiEvent is NoteEvent noteEvent))
                            return;
                        
                        Log.Debug($"{msg.MidiEvent.CommandCode}  NoteNumber: {noteEvent.NoteNumber}  Value: {noteEvent.Velocity}");
                        _signalsSinceLastUpdate.Add(new ButtonSignal()
                                                        {
                                                            Channel = noteEvent.Channel,
                                                            ButtonId = noteEvent.NoteNumber,
                                                            ControllerValue = noteEvent.Velocity,
                                                            State = msg.MidiEvent.CommandCode == MidiCommandCode.NoteOn
                                                                        ? ButtonSignal.States.JustPressed
                                                                        : ButtonSignal.States.Released,
                                                        });
                        return;

                    case MidiCommandCode.ControlChange:
                        if (!(msg.MidiEvent is ControlChangeEvent controlChangeEvent))
                            return;
                        
                        Log.Debug($"{msg.MidiEvent.CommandCode}  NoteNumber: {controlChangeEvent.Controller}  Value: {controlChangeEvent.ControllerValue}");
                        _controlSignalsSinceLastUpdate.Add(new ControlChangeSignal()
                                                               {
                                                                   Channel = controlChangeEvent.Channel,
                                                                   ControllerId = (int)controlChangeEvent.Controller,
                                                                   ControllerValue = controlChangeEvent.ControllerValue,
                                                               });
                        return;
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

        protected static void UpdateRangeLeds(MidiOut midiOut, ButtonRange range, ComputeColorForIndex computeColorForIndex)
        {
            foreach (var buttonIndex in range.Indices())
            {
                var mappedIndex = range.GetMappedIndex(buttonIndex);
                SendColor(midiOut, buttonIndex, computeColorForIndex(mappedIndex));
            }
        }

        private static void SendColor(MidiOut midiOut, int apcControlIndex, int colorCode)
        {
            if (CacheControllerColors[apcControlIndex] == colorCode)
                return;
            const int defaultChannel = 1;
            var noteOnEvent = new NoteOnEvent(0, defaultChannel, apcControlIndex, colorCode, 50);
            midiOut.Send(noteOnEvent.GetAsShortMessage());
            CacheControllerColors[apcControlIndex] = colorCode;
        }

        private static readonly int[] CacheControllerColors = Enumerable.Repeat(-1, 256).ToArray();
        #endregion

        private readonly Dictionary<int, ButtonSignal> _signalsForButtonCombination = new Dictionary<int, ButtonSignal>();
        private readonly List<ButtonSignal> _signalsSinceLastUpdate = new List<ButtonSignal>();
        private readonly List<ControlChangeSignal> _controlSignalsSinceLastUpdate = new List<ControlChangeSignal>();
    }
}