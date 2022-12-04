using System;
using System.Collections.Generic;
using NAudio.Midi;
using T3.Core.Logging;

namespace Operators.Utils
{
    public static class MidiInConnectionManager
    {
        public static void RegisterConsumer(IMidiConsumer consumer)
        {
            CloseMidiDevices();
            MidiConsumers.Add(consumer);
            ScanAndRegisterToMidiDevices();
        }
        
        public static void UnregisterConsumer(IMidiConsumer consumer)
        {
            if (!MidiConsumers.Contains(consumer))
                return;

            foreach (var midiIn in MidiInsWithDevices.Keys)
            {
                midiIn.MessageReceived -= consumer.MessageReceivedHandler;
                midiIn.ErrorReceived -= consumer.ErrorReceivedHandler;
            }

            MidiConsumers.Remove(consumer);
            if (MidiConsumers.Count == 0)
            {
                CloseMidiDevices();
            }
        }

        public static void Rescan()
        {
            CloseMidiDevices();
            ScanAndRegisterToMidiDevices(logInformation: true);
        }
        
        
        public interface IMidiConsumer
        {
            void MessageReceivedHandler(object sender, MidiInMessageEventArgs msg);
            void ErrorReceivedHandler(object sender, MidiInMessageEventArgs msg);
        }
        
        public static MidiInCapabilities GetDescriptionForMidiIn(MidiIn midiIn)
        {
            MidiInsWithDevices.TryGetValue(midiIn,  out  var description);
            return description;
        }

        public static MidiIn GetMidiInForProductNameHash(int hash)
        {
            MidiInsByDeviceIdHash.TryGetValue(hash, out var midiIn);
            return midiIn;
        }

        
        private static void ScanAndRegisterToMidiDevices(bool logInformation = false)
        {
            for (int index = 0; index < MidiIn.NumberOfDevices; index++)
            {
                var deviceInfo = MidiIn.DeviceInfo(index);

                var deviceInfoProductName = deviceInfo.ProductName;
                if (logInformation)
                    Log.Debug("Scanning " + deviceInfoProductName);

                MidiIn newMidiIn;
                try
                {
                    newMidiIn = new MidiIn(index);
                }
                catch (NAudio.MmException e)
                {
                    Log.Error(e.Message == "MemoryAllocationError"
                                  ? " > The device is already being used by an application"
                                  : $" > {e.Message} {deviceInfoProductName}");
                    continue;
                }

                foreach (var midiConsumer in MidiConsumers)
                {
                    newMidiIn.MessageReceived += midiConsumer.MessageReceivedHandler;
                    newMidiIn.ErrorReceived += midiConsumer.ErrorReceivedHandler;
                }

                newMidiIn.Start();
                MidiInsWithDevices[newMidiIn] = deviceInfo;
                MidiInsByDeviceIdHash[deviceInfoProductName.GetHashCode()] = newMidiIn;
            }
        }

        private static void CloseMidiDevices()
        {
            foreach (var midiInputDevice in MidiInsWithDevices.Keys)
            {
                foreach (var midiConsumer in MidiConsumers)
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

            MidiInsWithDevices.Clear();
            MidiInsByDeviceIdHash.Clear();
        }
        
        private static readonly List<IMidiConsumer> MidiConsumers = new List<IMidiConsumer>();
        private static readonly Dictionary<MidiIn, MidiInCapabilities> MidiInsWithDevices = new Dictionary<MidiIn, MidiInCapabilities>();
        private static readonly Dictionary<int, MidiIn> MidiInsByDeviceIdHash = new Dictionary<int, MidiIn>();
    }
}