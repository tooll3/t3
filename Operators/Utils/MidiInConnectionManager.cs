using System;
using System.Collections.Generic;
using NAudio.Midi;
using T3.Core.IO;
using T3.Core.Logging;

namespace Operators.Utils
{
    public static class MidiInConnectionManager
    {
        public static void RegisterConsumer(IMidiConsumer consumer)
        {
            if (!_initialized)
            {
                ScanAndRegisterToMidiDevices();
                _initialized = true;
            }

            if (_midiConsumers.Contains(consumer))
            {
                Log.Warning("MidiConsumer was already added " + consumer);
                return;
            }
            
            _midiConsumers.Add(consumer);
            
            foreach (var midiInputDevice in _midiInsWithDevices.Keys)
            {
                midiInputDevice.MessageReceived -= consumer.MessageReceivedHandler;
                midiInputDevice.ErrorReceived -= consumer.ErrorReceivedHandler;
                midiInputDevice.MessageReceived += consumer.MessageReceivedHandler;
                midiInputDevice.ErrorReceived += consumer.ErrorReceivedHandler;
            }
        }

        public static void UnregisterConsumer(IMidiConsumer consumer)
        {
            if (!_midiConsumers.Contains(consumer))
                return;

            foreach (var midiIn in _midiInsWithDevices.Keys)
            {
                midiIn.MessageReceived -= consumer.MessageReceivedHandler;
                midiIn.ErrorReceived -= consumer.ErrorReceivedHandler;
            }

            _midiConsumers.Remove(consumer);
            if (_midiConsumers.Count == 0)
            {
                CloseMidiDevices();
            }
        }

        public static void Rescan()
        { 
            
            CloseMidiDevices();
            ScanAndRegisterToMidiDevices(logInformation: true);
            
            // TODO: Clean up later
            foreach (var consumer in _midiConsumers)
            {
                foreach (var midiInputDevice in _midiInsWithDevices.Keys)
                {
                    midiInputDevice.MessageReceived -= consumer.MessageReceivedHandler;
                    midiInputDevice.ErrorReceived -= consumer.ErrorReceivedHandler;
                    midiInputDevice.MessageReceived += consumer.MessageReceivedHandler;
                    midiInputDevice.ErrorReceived += consumer.ErrorReceivedHandler;
                }
                consumer.OnSettingsChanged();
            }
        }
        
        
        public interface IMidiConsumer
        {
            void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg);
            void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg);
            
            /// <summary>
            /// This will be called if the number of controllers or devices changed and the
            /// listener should update its status.
            /// </summary>
            void OnSettingsChanged();
        }
        
        public static MidiInCapabilities GetDescriptionForMidiIn(MidiIn midiIn)
        {
            _midiInsWithDevices.TryGetValue(midiIn,  out  var description);
            return description;
        }

        public static MidiIn GetMidiInForProductNameHash(int hash)
        {
            _midiInsByDeviceIdHash.TryGetValue(hash, out var midiIn);
            return midiIn;
        }


        private static bool IsMidiDeviceCaptureEnabled(string deviceName)
        {
            var setting = ProjectSettings.Config.LimitMidiDeviceCapture;
            if (string.IsNullOrEmpty(setting))
                return true;

            foreach (var s in setting.Split("\n"))
            {
                if (deviceName.Contains(s.Trim(), StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }
        
        private static void ScanAndRegisterToMidiDevices(bool logInformation = false)
        {
            Log.Debug("Capturing Midi devices...");
            if (!string.IsNullOrEmpty(ProjectSettings.Config.LimitMidiDeviceCapture))
            {
                var settingsString = ProjectSettings.Config.LimitMidiDeviceCapture.Replace("\n", "; ");
                Log.Debug($"NOTE: In settings Midi device capture is limited to '{settingsString}");
            }
            
            for (var index = 0; index < MidiIn.NumberOfDevices; index++)
            {
                var deviceInputInfo = MidiIn.DeviceInfo(index);
                var deviceInfoProductName = deviceInputInfo.ProductName;

                if (!IsMidiDeviceCaptureEnabled(deviceInfoProductName))
                {
                    Log.Debug($" skipping '{deviceInfoProductName}' (disabled in setting)");
                    continue;
                }
                
                if (logInformation)
                    Log.Debug($" listening to '{deviceInfoProductName}'...");
                
                MidiIn newMidiIn;
                try
                {
                    newMidiIn = new MidiIn(index);
                }
                catch (NAudio.MmException e)
                {
                    Log.Error(e.Message == "MemoryAllocationError"
                                  ? " > The device is already being used by an application."
                                  : $" > {e.Message} {deviceInfoProductName}");
                    continue;
                }
                newMidiIn.Start();
                
                _midiInsWithDevices[newMidiIn] = deviceInputInfo;
                _midiInsByDeviceIdHash[deviceInfoProductName.GetHashCode()] = newMidiIn;
                
            }

            // MidiOut
            if (!ProjectSettings.Config.EnableMidiSnapshotIndication)
            {
                for (var index = 0; index < MidiOut.NumberOfDevices; index++)
                {
                    var deviceOutputInfo = MidiOut.DeviceInfo(index);
                    var deviceInfoProductName = deviceOutputInfo.ProductName;

                    if (!IsMidiDeviceCaptureEnabled(deviceInfoProductName))
                    {
                        Log.Debug($" skipping '{deviceInfoProductName}' (disabled in setting)");
                        continue;
                    }

                    MidiOut newMidiOut;
                    try
                    {
                        newMidiOut = new MidiOut(index);
                    }
                    catch (NAudio.MmException e)
                    {
                        Log.Error(e.Message == "MemoryAllocationError"
                                      ? " > The device is already being used by an application."
                                      : $" > {e.Message} {deviceInfoProductName}");
                        continue;
                    }

                    // foreach (var midiConsumer in _midiConsumers)
                    // {
                    //     newMidiIn.MessageReceived += midiConsumer.MessageReceivedHandler;
                    //     newMidiIn.ErrorReceived += midiConsumer.ErrorReceivedHandler;
                    // }

                    _midiOutsWithDevices[newMidiOut] = deviceOutputInfo;
                    _midiOutsByDeviceIdHash[deviceInfoProductName.GetHashCode()] = newMidiOut;
                }
            }

        }

        private static void CloseMidiDevices()
        {
            // Midi Ins
            foreach (var midiInputDevice in _midiInsWithDevices.Keys)
            {
                foreach (var midiConsumer in _midiConsumers)
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

            _midiInsWithDevices.Clear();
            _midiInsByDeviceIdHash.Clear();
            
            
            // Midi outs
            foreach (var midiOutputDevice in _midiOutsWithDevices.Keys)
            {
                // foreach (var midiConsumer in _midiConsumers)
                // {
                //     midiInputDevice.MessageReceived -= midiConsumer.MessageReceivedHandler;
                //     midiInputDevice.ErrorReceived -= midiConsumer.ErrorReceivedHandler;
                // }

                try
                {
                    midiOutputDevice.Close();
                    midiOutputDevice.Dispose();
                }
                catch (Exception e)
                {
                    Log.Debug("exception: " + e);
                }
            }

            _midiOutsWithDevices.Clear();
            _midiOutsByDeviceIdHash.Clear();
        }
        
        private static readonly List<IMidiConsumer> _midiConsumers = new();
        private static readonly Dictionary<MidiIn, MidiInCapabilities> _midiInsWithDevices = new();
        private static readonly Dictionary<int, MidiIn> _midiInsByDeviceIdHash = new();
        
        public static readonly Dictionary<MidiOut, MidiOutCapabilities> _midiOutsWithDevices = new();
        public static readonly Dictionary<int, MidiOut> _midiOutsByDeviceIdHash = new();
        private static bool _initialized;
    }
}