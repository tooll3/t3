using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using Operators.Utils;
using T3.Core.Logging;
using T3.Editor.Gui.Interaction.Variations.Midi.CommandProcessing;
using T3.Editor.Gui.Interaction.Variations.Model;

namespace T3.Editor.Gui.Interaction.Variations.Midi
{
    public interface IControllerInputDevice
    {
        void Update(MidiIn midiIn, Variation activeVariation);
        int GetProductNameHash();
    }

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
            Delete = 1 << 2,
            Save = 1 << 3,
            BlendTo = 1 << 4,
            None = 0,
        }

        public InputModes ActiveMode = InputModes.Default;

        public virtual void Update(MidiIn midiIn, Variation activeVariation)
        {
            CombineButtonSignals();

            ControlChangeSignal[] controlChangeSignals = null;

            lock (_controlSignalsSinceLastUpdate)
            {
                controlChangeSignals = _controlSignalsSinceLastUpdate.ToArray();
                _controlSignalsSinceLastUpdate.Clear();
            }

            foreach (var ctc in CommandTriggerCombinations)
            {
                ctc.InvokeMatchingControlCommands(controlChangeSignals, ActiveMode);
            }

            if (_combinedButtonSignals.Count == 0)
                return;

            var releasedMode = InputModes.None;

            // Update modes
            if (ModeButtons != null)
            {
                foreach (var modeButton in ModeButtons)
                {
                    var matchingSignal = _combinedButtonSignals.Values.SingleOrDefault(s => modeButton.ButtonRange.IncludesButtonIndex(s.ButtonId));
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
            }

            if (CommandTriggerCombinations == null)
                return;

            var isAnyButtonPressed = _combinedButtonSignals.Values.Any(signal => (signal.State == ButtonSignal.States.JustPressed
                                                                                  || signal.State == ButtonSignal.States.Hold));

            foreach (var ctc in CommandTriggerCombinations)
            {
                ctc.InvokeMatchingButtonCommands(_combinedButtonSignals.Values.ToList(), ActiveMode, releasedMode);
            }

            if (!isAnyButtonPressed)
            {
                _combinedButtonSignals.Clear();
            }
        }

        public abstract int GetProductNameHash();

        protected List<CommandTriggerCombination> CommandTriggerCombinations;
        protected List<ModeButton> ModeButtons;

        // ------------------------------------------------------------------------------------
        #region Process button Signals
        private void CombineButtonSignals()
        {
            lock (_buttonSignalsSinceLastUpdate)
            {
                foreach (var earlierSignal in _combinedButtonSignals.Values)
                {
                    if (earlierSignal.State == ButtonSignal.States.JustPressed)
                        earlierSignal.State = ButtonSignal.States.Hold;
                }

                foreach (var newSignal in _buttonSignalsSinceLastUpdate)
                {
                    if (_combinedButtonSignals.TryGetValue(newSignal.ButtonId, out var earlierSignal))
                    {
                        earlierSignal.State = newSignal.State;
                    }
                    else
                    {
                        _combinedButtonSignals[newSignal.ButtonId] = newSignal;
                    }
                }

                _buttonSignalsSinceLastUpdate.Clear();
            }
        }

        void MidiInConnectionManager.IMidiConsumer.OnSettingsChanged()
        {
        }

        void MidiInConnectionManager.IMidiConsumer.MessageReceivedHandler(object sender, MidiInMessageEventArgs msg)
        {
            lock (this)
            {
                if (!(sender is MidiIn midiIn) || msg.MidiEvent == null)
                    return;

                var device = MidiInConnectionManager.GetDescriptionForMidiIn(midiIn);

                if (device.ProductName.GetHashCode() != GetProductNameHash())
                {
                    //Log.Debug($"Ingore device {device.ProductName} in AbstractMidiController");
                    return;
                }

                if (msg.MidiEvent == null)
                    return;

                switch (msg.MidiEvent.CommandCode)
                {
                    case MidiCommandCode.NoteOff:
                    case MidiCommandCode.NoteOn:
                        if (!(msg.MidiEvent is NoteEvent noteEvent))
                            return;

                        //Log.Debug($"{msg.MidiEvent.CommandCode}  NoteNumber: {noteEvent.NoteNumber}  Value: {noteEvent.Velocity}");
                        _buttonSignalsSinceLastUpdate.Add(new ButtonSignal()
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

                        lock (_controlSignalsSinceLastUpdate)
                        {
                            //Log.Debug($"{msg.MidiEvent.CommandCode}  NoteNumber: {controlChangeEvent.Controller}  Value: {controlChangeEvent.ControllerValue}");
                            _controlSignalsSinceLastUpdate.Add(new ControlChangeSignal()
                                                                   {
                                                                       Channel = controlChangeEvent.Channel,
                                                                       ControllerId = (int)controlChangeEvent.Controller,
                                                                       ControllerValue = controlChangeEvent.ControllerValue,
                                                                   });
                        }

                        return;
                }
            }
        }

        void MidiInConnectionManager.IMidiConsumer.ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg)
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
            try
            {
                midiOut.Send(noteOnEvent.GetAsShortMessage());
            }
            catch(NAudio.MmException e) 
            {
                Log.Warning("Failed setting midi color message:" + e.Message);        
            } 
            CacheControllerColors[apcControlIndex] = colorCode;
        }

        private static readonly int[] CacheControllerColors = Enumerable.Repeat(-1, 256).ToArray();
        #endregion

        private readonly Dictionary<int, ButtonSignal> _combinedButtonSignals = new();
        private readonly List<ButtonSignal> _buttonSignalsSinceLastUpdate = new();
        private readonly List<ControlChangeSignal> _controlSignalsSinceLastUpdate = new();
    }
}