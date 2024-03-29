using System;
using System.Collections.Generic;
using System.Reflection;
using Operators.Utils;
using T3.Core.Logging;
using T3.Editor.Gui.Interaction.Midi.CompatibleDevices;
using Type = System.Type;

namespace T3.Editor.Gui.Interaction.Midi;

/// <summary>
/// Handles the initialization and update of <see cref="CompatibleMidiDevice"/>s.
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

        // Dispose devices
        foreach (var device in _connectedMidiDevices)
        {
            device.Dispose();
        }

        _connectedMidiDevices.Clear();
        
        CreateConnectedCompatibleDevices();
    }

    public static void UpdateConnectedDevices()
    {
        foreach (var compatibleMidiDevice in _connectedMidiDevices)
        {
            compatibleMidiDevice.Update();
        }
    }

    /// <summary>
    /// Creates instances for connected known controller types.
    /// </summary>
    private static void CreateConnectedCompatibleDevices()
    {
        foreach (var controllerType in _compatibleControllerTypes)
        {
            var attr = controllerType.GetCustomAttribute<MidiDeviceProductAttribute>(false);
            if (attr == null)
            {
                Log.Warning($"{controllerType} should implement MidiDeviceProductAttribute");
                continue;
            }

            var productName = attr.ProductName;

            foreach (var (midiIn, midiInCapabilities) in MidiInConnectionManager.MidiIns)
            {
                if (midiInCapabilities.ProductName != productName)
                    continue;

                if (Activator.CreateInstance(controllerType) is not CompatibleMidiDevice compatibleDevice)
                {
                    Log.Warning("Can't create midi-device");
                    return;
                }

                compatibleDevice.Initialize(productName, midiIn);
                _connectedMidiDevices.Add(compatibleDevice);
                Log.Debug($"Connected midi device {compatibleDevice}");
            }
        }
    }

    // TODO: This list could be inferred by reflection checking for MidiDeviceProductAttribute 
    private static readonly List<Type> _compatibleControllerTypes
        = new()
              {
                  typeof(Apc40Mk2),
                  typeof(ApcMini),
                  typeof(NanoControl8)
              };

    private static readonly List<CompatibleMidiDevice> _connectedMidiDevices = new();
}