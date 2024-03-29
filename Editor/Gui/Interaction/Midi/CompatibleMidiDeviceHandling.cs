using System;
using System.Collections.Generic;
using NAudio.Midi;
using Operators.Utils;
using T3.Core.Logging;
using T3.Editor.Gui.Interaction.Midi.CompatibleDevices;
using T3.Editor.Gui.Interaction.Variations;
using Type = System.Type;

namespace T3.Editor.Gui.Interaction.Midi;

/// <summary>
/// Handles the initialization and update of compatible midi devices.
/// </summary>
public static class CompatibleMidiDeviceHandling
{
    public static void InitializeConnectedDevices()
    {
        if (!MidiInConnectionManager.Initialized)
        {
            //Log.Warning("MidiInConnectionManager should be initialized before InitializeConnectedDevices().");
            MidiInConnectionManager.Rescan();
        }
            
        _connectedMidiDevices.Clear();
        foreach (var c in _compatibleControllerTypes)
        {
            RegisterControllerType(c);
        }
    }

    public static void UpdateConnectedDevices()
    {
        if (VariationHandling.ActivePoolForSnapshots == null)
            return;

        foreach (var (midiIn, abstractMidiDevice) in _connectedMidiDevices)
        {
            abstractMidiDevice.UpdateVariationHandling(midiIn, VariationHandling.ActivePoolForSnapshots.ActiveVariation);
        }
    }
    
    /// <summary>
    /// Creates instances for connected known controller types.
    /// </summary>
    /// <remarks>
    /// This implementation should be adjusted to support multiple connected controllers
    /// of the same type: Ideally we should iterate over the connected midiIns and then look for
    /// and instantiate matching controller.
    /// </remarks>
    private static void RegisterControllerType(Type controller)
    {
        // Creating an instance just to access the static product name hash feels ugly...
        if (Activator.CreateInstance(controller) is not CompatibleMidiDevice compatibleDevice)
        {
            Log.Warning("Can't create midi-device");
            return;
        }

        var productNameHash = compatibleDevice.GetProductNameHash();
        var midiIn = MidiInConnectionManager.GetMidiInForProductNameHash(productNameHash);
        if (midiIn == null)
            return;

        Log.Debug($"Connected midi device {compatibleDevice}");
        _connectedMidiDevices.Add(new Tuple<MidiIn, CompatibleMidiDevice>(midiIn, compatibleDevice));
    }



    private static readonly List<Tuple<MidiIn, CompatibleMidiDevice>> _connectedMidiDevices = new();

    private static readonly List<Type> _compatibleControllerTypes
        = new()
              {
                  typeof(Apc40Mk2),
                  typeof(ApcMini),
                  typeof(NanoControl8)
              };
}