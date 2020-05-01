using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using ManagedBass;
using ManagedBass.Wasapi;
using T3.Core.Logging;

namespace T3.Gui.Interaction.Timing
{
    /// <summary>
    /// This is based on https://www.codeproject.com/Articles/797537/
    /// Audio Spectrum by @webmaster442
    /// </summary>
    public class SystemAudioInput
    {
        public readonly float[] LastFftBuffer = new float[512];
        public float LastLevel => (RightLevel + LeftLevel) / 2;

        public void Restart()
        {
            Log.Warning("Looks like sound got lost. Trying to restart.");
            _hangCounter = 0;
            Free();

            InitBass();
            _initialized = false;
            SetEnableWasapi(true);
        }

        public float RightLevel;
        public float LeftLevel;
        public int SelectedDeviceIndex { get; private set; }
        public readonly List<WasapiDeviceDescription> LoopBackDevices = new List<WasapiDeviceDescription>();

        public struct WasapiDeviceDescription
        {
            public string Title;
            public string Type;
            public int WasapiDeviceIndex;

            public override string ToString()
            {
                return $"#{WasapiDeviceIndex} {Title} {Type}";
            }
        }

        /// <summary>
        /// Note that the initialization of the the WASAPI device list can
        /// take several seconds. So this class should only be instantiated on demand.</summary>
        public SystemAudioInput()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(1 / 44.1f * 1000);
            _timer.Tick += TimerUpdateEventHandler;
            // ReSharper disable once RedundantDelegateCreation
            _wasapiProcedure = new WasapiProcedure(Process); // capture to avoid freeing by GC
            _initialized = false;
            Init();
        }

        public void SetDeviceIndex(int index)
        {
            if (index == SelectedDeviceIndex)
                return;

            SetEnableWasapi(false);
            SelectedDeviceIndex = index;
            _initialized = false;
            SetEnableWasapi(true);
        }

        private void SetEnableWasapi(bool enable)
        {
            if (!enable)
            {
                BassWasapi.Stop();
                BassWasapi.Free();
                _timer.Stop();
                _timer.IsEnabled = false;
                return;
            }

            if (!_initialized)
            {
                var selectedDeviceDescription = LoopBackDevices[SelectedDeviceIndex % LoopBackDevices.Count];

                //var device = BassWasapi.GetDeviceInfo(selectedDeviceId);
                Log.Debug($"Initializing WASAPI for selection #{SelectedDeviceIndex}  -> {selectedDeviceDescription}");
                if (!BassWasapi.Init(selectedDeviceDescription.WasapiDeviceIndex,
                                     Frequency: 0,
                                     Channels: 0,
                                     Flags: WasapiInitFlags.Buffer,
                                     Buffer: 0.004f, // was 1
                                     Period: 0.004f,
                                     Procedure: _wasapiProcedure,
                                     User: IntPtr.Zero))
                {
                    Log.Error("Can't initialize WASAPI:" + Bass.LastError);
                    return;
                }

                _initialized = true;
            }

            BassWasapi.Start();
            System.Threading.Thread.Sleep(500);
            _timer.IsEnabled = true;
            _timer.Start();
        }

        private void Init()
        {
            InitBass();
            SetEnableWasapi(true);
        }

        private void InitBass()
        {
            for (var index = 0; index < BassWasapi.DeviceCount; index++)
            {
                var deviceInfo = BassWasapi.GetDeviceInfo(index);
                if (!deviceInfo.IsEnabled || !deviceInfo.IsLoopback)
                    continue;

                LoopBackDevices.Add(new WasapiDeviceDescription()
                                        {
                                            WasapiDeviceIndex = index,
                                            Title = deviceInfo.Name,
                                            Type = deviceInfo.Type.ToString(),
                                        });
                Log.Debug($"Found #{index} - {deviceInfo.Name}");
            }

            Bass.Configure(Configuration.UpdateThreads, false);

            var result = Bass.Init(Device: 0, Frequency: 44100, Flags: DeviceInitFlags.Default, Win: IntPtr.Zero);
            if (result)
            {
                Log.Debug("Successfully initialized BASS.Init()");
            }

            if (!result)
            {
                Log.Error("Bass initialization failed:" + Bass.LastError);
            }
        }

        private void TimerUpdateEventHandler(object sender, EventArgs e)
        {
            BassWasapi.GetData(null, (int)DataFlags.Available);

            // get FFT data. Return value is -1 on error
            var ret = BassWasapi.GetData(LastFftBuffer, (int)DataFlags.FFT512);
            if (ret < 0)
                return;

            // Get audio level
            var level = BassWasapi.GetLevel();
            var left = level &= 0xffff;
            var right = level >> 16;
            LeftLevel = 65384f / left;
            RightLevel = 65384f / right;

            if (level == LastIntLevel && level != 0) _hangCounter++;
            LastIntLevel = level;

            // Required, because some programs hang the output. If the output hangs for a 75ms
            // this piece of code re initializes the output so it doesn't make a glitched sound for long.
            //
            // This is a pain in the butt, and I already sunk some time into this problem.
            // It's definitely worse in when streaming audio/video in web browsers like Firefox. 
            if (_hangCounter > 120)
            {
                Restart();
            }
        }

        /// <summary>
        /// WASAPI callback, required for continuous recording
        /// </summary>
        private static int Process(IntPtr buffer, int length, IntPtr user)
        {
            return length;
        }

        //cleanup
        private static void Free()
        {
            BassWasapi.Free();
            Bass.Free();
        }

        //private const int FftBufferResolution = 512;

        private readonly DispatcherTimer _timer = new DispatcherTimer(); //timer that refreshes the display

        private readonly WasapiProcedure _wasapiProcedure;
        public int LastIntLevel;
        private int _hangCounter;

        private bool _initialized;
    }
}