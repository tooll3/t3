using System;
using System.Collections.Generic;
using System.Linq;
using ManagedBass;
using ManagedBass.Wasapi;
using T3.Core.IO;
using T3.Core.Logging;

namespace Core.Audio
{
    /// <summary>
    /// Uses the windows Wasapi audio API to get audio reaction from devices like speakers and microphones
    /// </summary>
    public static class WasapiAudioInput
    {
        /// <summary>
        /// Initializes the default device list and input.
        /// NOTE This can take several seconds(!)
        /// </summary>
        public static void Initialize()
        {
            if (_inputDevices != null)
                return;
            
            InitializeInputDeviceList();

            var deviceName = ProjectSettings.Config.AudioInputDeviceName;
            var device = _inputDevices.FirstOrDefault(d => d.DeviceInfo.Name == deviceName);
            StartInputCapture(device);
        }
        
        public static List<WasapiInputDevice> InputDevices
        {
            get
            {
                if(_inputDevices == null)
                    InitializeInputDeviceList();

                return _inputDevices;
            }
        }
        
        private static List<WasapiInputDevice> _inputDevices;

        public static void CompleteFrame()
        {
            _fftUpdatesSinceLastFrame = 0;
        }
        
        /// <summary>
        /// If device is null we will attempt default input index
        /// </summary>
        public static void StartInputCapture(WasapiInputDevice device)
        {
            int inputDeviceIndex = BassWasapi.DefaultInputDevice;
            AudioAnalysis.InputMode = AudioAnalysis.InputModes.WasapiDevice;
            
            if (device == null)
            {
                if (_inputDevices.Count == 0)
                {
                    Log.Error("No wasapi input devices found");
                    return;
                }
                Log.Error($"Attempting default input {BassWasapi.DefaultInputDevice}.");
                device = _inputDevices[0];
            }
            else
            {
                Log.Debug($"Initializing WASAPI audio input for  {device.DeviceInfo.Name}... ");
                inputDeviceIndex = device.WasapiDeviceIndex;
            }
            
            Bass.Configure(Configuration.UpdateThreads, false);
            // Bass.Configure(Configuration.DeviceBufferLength, 1024);

            BassWasapi.Stop();
            BassWasapi.Free();
            if (!BassWasapi.Init(inputDeviceIndex,
                                 Frequency: 0,
                                 Channels: 0,
                                 //Flags: WasapiInitFlags.Buffer | WasapiInitFlags.Exclusive,
                                 Flags: WasapiInitFlags.Buffer,
                                 Buffer: (float)device.DeviceInfo.DefaultUpdatePeriod,
                                 Period: (float)device.DeviceInfo.DefaultUpdatePeriod,
                                 Procedure: _wasapiProcedure,
                                 User: IntPtr.Zero))
            {
                Log.Error("Can't initialize WASAPI:" + Bass.LastError);
                return;
            }
            
            BassWasapi.Start();
            AudioAnalysis.InputMode = AudioAnalysis.InputModes.WasapiDevice;
        }

        public static void StopInputCapture()
        {
            //_assignedTimingHandler = false;
        }


        public static bool DevicesInitialized => _inputDevices != null;
        
        public static void InitializeInputDeviceList()
        {
            _inputDevices = new List<WasapiInputDevice>();
            
            // Keep in local variable to avoid double evaluation
            var deviceCount = BassWasapi.DeviceCount;
            
            for (var deviceIndex = 0; deviceIndex < deviceCount; deviceIndex++)
            {
                var deviceInfo = BassWasapi.GetDeviceInfo(deviceIndex);
                var isValidInputDevice = deviceInfo.IsEnabled && (deviceInfo.IsLoopback || deviceInfo.IsInput);

                if (!isValidInputDevice)
                    continue;

                Log.Debug($"Found Wasapi input ID:{_inputDevices.Count} {deviceInfo.Name} LoopBack:{deviceInfo.IsLoopback} IsInput:{deviceInfo.IsInput} (at {deviceIndex})");
                _inputDevices.Add(new WasapiInputDevice()
                                    {
                                        WasapiDeviceIndex = deviceIndex,
                                        DeviceInfo = deviceInfo,
                                    });
            }
        }
        
        private static int Process(IntPtr buffer, int length, IntPtr user)
        {
            var level = BassWasapi.GetLevel();
            if (length < 3000)
                 return length;

            int resultCode;
            if (_fftUpdatesSinceLastFrame == 0)
            {
                resultCode = BassWasapi.GetData(AudioAnalysis.FftGainBuffer, (int)(AudioAnalysis.BassFlagForFftBufferSize | DataFlags.FFTRemoveDC));
            }
            else
            {
                resultCode = BassWasapi.GetData(_fftIntermediate, (int)(AudioAnalysis.BassFlagForFftBufferSize | DataFlags.FFTRemoveDC));
                if (resultCode >= 0)
                {
                    for (var i = 0; i < AudioAnalysis.FftHalfSize; i++)
                    {
                        AudioAnalysis.FftGainBuffer[i] = MathF.Max(_fftIntermediate[i], AudioAnalysis.FftGainBuffer[i]);
                    }
                }
            }
            
            if (resultCode < 0)
            {
                Log.Debug($"Can't get Wasapi FFT-Data: {Bass.LastError}");
            }

            var audioLevel = level * 0.00001;
            _fftUpdatesSinceLastFrame++;
            //Log.Debug($"Process with {length} #{_fftUpdatesSinceLastFrame}  L:{audioLevel:0.0}  DevBufLen:{BassWasapi.Info.BufferLength}");
            return length;
        }

        private static int _fftUpdatesSinceLastFrame;
        
        public class WasapiInputDevice
        {
            public int WasapiDeviceIndex;
            public WasapiDeviceInfo DeviceInfo;
        }

        private static readonly float[] _fftIntermediate = new float[AudioAnalysis.FftHalfSize];
        private static readonly WasapiProcedure _wasapiProcedure = Process;
    }
}