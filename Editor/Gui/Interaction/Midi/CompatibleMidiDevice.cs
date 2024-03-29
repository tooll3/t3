using System;
using System.Collections.Generic;
using System.Linq;
using NAudio;
using NAudio.Midi;
using Operators.Utils;
using T3.Core.Logging;
using T3.Editor.Gui.Interaction.Midi.CommandProcessing;
using T3.Editor.Gui.Interaction.Variations;
using T3.Editor.Gui.Interaction.Variations.Model;

namespace T3.Editor.Gui.Interaction.Midi;

/// <summary>
/// Combines midi signals related to Variations into triggers and invokes matching <see cref="CommandTriggerCombination"/>s.
/// Allow allows to update the status of midi devices, e.g. for controlling LEDs to indicate available or active variations.
/// </summary>
/// <remarks>
/// This is NOT related to the MidiInput operator: Both are registered as independent <see cref="MidiInConnectionManager.IMidiConsumer"/>
/// and handle their events individually.
/// </remarks>
public abstract class CompatibleMidiDevice : MidiInConnectionManager.IMidiConsumer, IDisposable
{
    public void Initialize(string productName, MidiIn midiIn)
    {
        _productName = productName;
        _midiInputDevice = midiIn;
        MidiInConnectionManager.RegisterConsumer(this);
    }

    /// <summary>
    /// Depending on various hotkeys a device can be in different input modes.
    /// This allows actions with the button combinations.
    /// </summary>
    [Flags]
    public enum InputModes
    {
        Default = 1 << 1,
        Delete = 1 << 2,
        Save = 1 << 3,
        BlendTo = 1 << 4,
        None = 0,
    }

    protected InputModes ActiveMode = InputModes.Default;

    protected abstract void UpdateVariationVisualization();

    public void Update()
    {
        UpdateVariationVisualization();
        
        CombineButtonSignals();
        ProcessLastSignals();
        _hasNewMessages = false;
    }

    private void ProcessLastSignals()
    {
        if (!_hasNewMessages)
            return;

        ControlChangeSignal[] controlChangeSignals;
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

    public void Dispose()
    {
        MidiInConnectionManager.UnregisterConsumer(this);
    }

    protected List<CommandTriggerCombination> CommandTriggerCombinations;
    protected List<ModeButton> ModeButtons;
    private bool _hasNewMessages;

    // ------------------------------------------------------------------------------------
    #region Process button Signals
    /// <summary>
    /// Combines press/hold/release signals into states like JustPressed and Hold than are
    /// later used to check for actions triggered by button combinations. 
    /// </summary>
    private void CombineButtonSignals()
    {
        if (!_hasNewMessages)
            return;
        
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
        if (sender is not MidiIn midiIn || msg.MidiEvent == null)
            return;

        if (midiIn != _midiInputDevice)
            return;

        if (msg.MidiEvent == null)
            return;

        switch (msg.MidiEvent.CommandCode)
        {
            case MidiCommandCode.NoteOff:
            case MidiCommandCode.NoteOn:
                if (msg.MidiEvent is NoteEvent noteEvent)
                {
                    lock (_buttonSignalsSinceLastUpdate)
                    {
                        _buttonSignalsSinceLastUpdate.Add(new ButtonSignal()
                                                              {
                                                                  Channel = noteEvent.Channel,
                                                                  ButtonId = noteEvent.NoteNumber,
                                                                  ControllerValue = noteEvent.Velocity,
                                                                  State = msg.MidiEvent.CommandCode == MidiCommandCode.NoteOn
                                                                              ? ButtonSignal.States.JustPressed
                                                                              : ButtonSignal.States.Released,
                                                              });
                    }
                    _hasNewMessages = true;
                }
                return;
            //Log.Debug($"{msg.MidiEvent.CommandCode}  NoteNumber: {noteEvent.NoteNumber}  Value: {noteEvent.Velocity}");

            case MidiCommandCode.ControlChange:
                if (msg.MidiEvent is not ControlChangeEvent controlChangeEvent)
                    return;

                lock (_controlSignalsSinceLastUpdate)
                {
                    _controlSignalsSinceLastUpdate.Add(new ControlChangeSignal()
                                                           {
                                                               Channel = controlChangeEvent.Channel,
                                                               ControllerId = (int)controlChangeEvent.Controller,
                                                               ControllerValue = controlChangeEvent.ControllerValue,
                                                           });
                }
                _hasNewMessages = true;
                return;
        }
    }

    void MidiInConnectionManager.IMidiConsumer.ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg)
    {
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
        catch (MmException e)
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
    private MidiIn _midiInputDevice;
    protected int ProductNameHash => _productName.GetHashCode();
    private string _productName;
}

public class MidiDeviceProductAttribute : Attribute
{
    public MidiDeviceProductAttribute(string productName)
    {
        ProductName = productName;
    }

    public string ProductName { get; set; }
}