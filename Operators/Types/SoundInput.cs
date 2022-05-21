using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ManagedBass;
using ManagedBass.Wasapi;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_b72d968b_0045_408d_a2f9_5c739c692a66
{
    public class SoundInput : Instance<SoundInput>
    {
        [Output(Guid = "B3EFDF25-4692-456D-AA48-563CFB0B9DEB", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<List<float>> FftBuffer = new();

        [Output(Guid = "b438986f-6ef9-40d5-8a2c-b00c01578ebc", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> Result = new();

        [Output(Guid = "D7D2A87C-4231-4F8B-904F-6E5F5D01B1D8", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> AvailableData = new();

        public SoundInput()
        {
            Result.UpdateAction = Update;
            FftBuffer.UpdateAction = Update;
            AvailableData.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if(Utilities.DetectHit(Reset.GetValue(context), ref _wasReset))
            {
                _analyzer?.ReInit();
            }

            if (_analyzer == null)
            {
                // if(_analyzer != null)
                //     _analyzer.Dispose();
                
                _analyzer = new Analyzer();
            }
            
            _analyzer.SetDeviceIndex(DeviceIndex.GetValue(context));
            
            var gain = Gain.GetValue(context);
            var isGainSet = Math.Abs(gain - 1) > 0.001f;
            if (isGainSet)
            {
                FftBuffer.Value ??= new List<float>();
                FftBuffer.Value.Clear();
                foreach (var f in _analyzer.FftBuffer)
                {
                    FftBuffer.Value.Add(f * gain);
                }
            }
            else
            {
                FftBuffer.Value = _analyzer.FftBuffer.ToList();
            }
            AvailableData.Value = _analyzer.AvailableData;
        }
        
        protected override void Dispose(bool disposing)
        {
            _analyzer.Dispose();
        }

        private static Analyzer _analyzer;
        private bool _wasReset;

        [Input(Guid = "41CFDC83-DC6B-4CC3-B814-C60177EF013F")]
        public readonly InputSlot<int> DeviceIndex = new();
        
        [Input(Guid = "E278C4C8-4C3A-4F95-BEEC-CE8EAD827724")]
        public readonly InputSlot<bool> Reset = new();
        
        [Input(Guid = "BCD6C387-7FEC-494F-B956-560728F05BFF")]
        public readonly InputSlot<float> Gain = new();
    }
    
    
    
    /// <summary>
    /// This is based on https://www.codeproject.com/Articles/797537/
    /// Audio Spectrum by @webmaster442
    /// </summary>
    internal class Analyzer : IDisposable
    {
        public Analyzer()
        {
            _timer.Interval = 1000.0/120.0;
            _timer.Elapsed += TimerUpdateEventHandler;
            
            // ReSharper disable once RedundantDelegateCreation
            _wasapiProcedure = new WasapiProcedure(Process); // capture to avoid freeing by GC
            UpdateDeviceList();
        }

        public void Dispose()
        {
            Log.Debug("Disposing Sound analyser");
            _timer.Elapsed -= TimerUpdateEventHandler;
            Free();
        }
        
        public void ReInit()
        {
            Stop();
            UpdateDeviceList();
            Start();
        }        

        public void SetDeviceIndex(int index)
        {
             if (index == _deviceIndex)
                 return;

             Stop();
             _deviceIndex = index;
             Start();
        }
        
        private void Start()
        {
            var saveDeviceIndex = Math.Abs(_deviceIndex) % _deviceList.Count;
            if (_deviceList.Count == 0)
            {
                Log.Error("Can't initialize empty device list.");
                return;
            }
                
            var deviceInfo = _deviceList[ saveDeviceIndex];
            
            _wasapiDeviceIndex = deviceInfo.Index;
            
            Log.Debug($"Initializing WASAPI for {saveDeviceIndex} {deviceInfo.Name}... #{_wasapiDeviceIndex}");
            Bass.Configure(Configuration.UpdateThreads, false);
            
            if (!BassWasapi.Init(_wasapiDeviceIndex, 
                                 Frequency: 0, 
                                 Channels: 0,
                                 //Flags: WasapiInitFlags.Buffer | WasapiInitFlags.Exclusive,
                                 Flags: WasapiInitFlags.Buffer,
                                 Buffer: 1,//0.0041f/2, // was 1
                                 Period: 1, //0.0041f/2,
                                 Procedure: _wasapiProcedure, 
                                 User: IntPtr.Zero))
            {
                Log.Error("Can't initialize WASAPI:" + Bass.LastError);
                return;
            }

            BassWasapi.Start();
            System.Threading.Thread.Sleep(100);
            _timer.Enabled = true;
            _timer.Start();
            Log.Debug(" Starting?");
            _noDataErrorShownOnce = false;

        }
        
        private void Stop()
        {
            BassWasapi.Stop();
            //BassWasapi.Free();    // Careful. Freeing Wasipi will disrupt T3 audio playback! 
            _timer.Stop();
            _timer.Enabled = false;
        }
        
        private void UpdateDeviceList()
        {
            _deviceList.Clear();
            for (var deviceIndex = 0; deviceIndex < BassWasapi.DeviceCount; deviceIndex++)
            {
                var device = BassWasapi.GetDeviceInfo(deviceIndex);
                var isValidInputDevice = device.IsEnabled && (device.IsLoopback || device.IsInput);

                if (!isValidInputDevice)
                    continue;

                Log.Debug($"Found Wasapi input ID:{_deviceList.Count} {device.Name} LoopBack:{device.IsLoopback} IsInput:{device.IsInput} (at {deviceIndex})");
                _deviceList.Add(new DeviceInfo
                                    {
                                        Index = deviceIndex,
                                        Name = device.Name
                                    });
            }
        }

        private bool _noDataErrorShownOnce = false; 
            
        private void TimerUpdateEventHandler(object sender, EventArgs eventArgs)
        {
            AvailableData = BassWasapi.GetData(null, (int)DataFlags.Available);

            // Note: The DataFlags seems to be offset by one (e.g. FFT256 only fills 128 entries)
            const int get256FftValues = (int)DataFlags.FFT512; 
            
            // Get FFT data. Return value is -1 on error
            var result = BassWasapi.GetData(FftBuffer, get256FftValues);
            if (result < 0 && !_noDataErrorShownOnce)
            {
                Log.Debug($"No new FFT-Data: {Bass.LastError}");
                _noDataErrorShownOnce = true;
                return;
            }

            if (_noDataErrorShownOnce)
            {
                Log.Debug($"Data available again?: {Bass.LastError}");
                _noDataErrorShownOnce = false;
            }
            var level = BassWasapi.GetLevel();

            // Required, because some programs hang the output. If the output hangs for a 75ms
            // this piece of code re initializes the output so it doesn't make a glitched sound for long.
            if (level == _lastLevel && level > 0)
            {
                if (_hangCounter++ <= 10)
                    return;
                
                Log.Warning("Looks like sound got lost. Trying to restart.");
                _hangCounter = 0;
                Free();
                Start();
                return;
            }

            _lastLevel = level;
            _hangCounter = 0;
        }

        /// <summary>
        /// WASAPI callback, required for continuous recording
        /// </summary>
        private static int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }
        
        private static void Free()
        {
            BassWasapi.Free();
            Bass.Free();
        }
        
        private struct DeviceInfo
        {
            public int Index;
            public string Name;
        }
        
        /// <summary>
        /// This property returns the length of the available data.
        /// This might be useful for eventually discover the reason for
        /// the frequent connection problems to sound provides like streaming from Firefox 
        /// </summary>
        public float AvailableData { get; private set; }
        public readonly float[] FftBuffer =  new float[FftSize];
        
        private int _deviceIndex = -1;  // not initialized
        private int _lastLevel;
        private readonly Timer _timer = new();
        private readonly WasapiProcedure _wasapiProcedure;
        private int _hangCounter;
        private const int FftSize = 256;
        private readonly List<DeviceInfo> _deviceList = new();
        private int _wasapiDeviceIndex;
    }
}