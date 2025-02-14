using System;
using System.Collections.Generic;
using System.Linq;
using ManagedBass;
using ManagedBass.Wasapi;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;

namespace T3.Core.Audio;

/// <summary>
/// Uses the windows Wasapi audio API to get audio reaction from devices like speakers and microphones
/// </summary>
public static class WasapiAudioInput
{
    /// <summary>
    /// Needs to be called once a frame.
    /// It switches audio input device if required
    /// </summary>
    public static void StartFrame(PlaybackSettings settings)
    {
        _fftUpdatesSinceLastFrame = 0;
            
        if (settings == null)
            return;
                    
        if (settings.AudioSource != PlaybackSettings.AudioSources.ExternalDevice)
        {
            if (!string.IsNullOrEmpty(ActiveInputDeviceName))
            {
                Stop();
            }
            return ;
        }

        var deviceName = settings.AudioInputDeviceName;
        if (ActiveInputDeviceName == deviceName)
        {
            // Try to restart capture
            if(!_failedToGetLastFffData)
                return;

            Log.Debug("Trying to restart WASAPI...");
            _failedToGetLastFffData = false;
        }
            
        if (string.IsNullOrEmpty(deviceName))
        {
            if (_complainedOnce)
                return ;
                
            Log.Warning("Can't switch to WASAPI device without a name");
            _complainedOnce = true;
            return ;
        }


        var device = InputDevices.FirstOrDefault(d => d.DeviceInfo.Name == deviceName);
        if (device == null)
        {
            Log.Warning($"Can't find input device {deviceName}");
            _complainedOnce = true;
            return ;
        }

        StartInputCapture(device);
        _complainedOnce = false;
    }


    public static List<WasapiInputDevice> InputDevices
    {
        get
        {
            if (_inputDevices == null)
                InitializeInputDeviceList();

            return _inputDevices;
        }
    }



    /// <summary>
    /// If device is null we will attempt default input index
    /// </summary>
    private static void StartInputCapture(WasapiInputDevice device)
        //public static void StartInputCapture(string deviceName)
    {
        int inputDeviceIndex = BassWasapi.DefaultInputDevice;

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
            Log.Info($"Initializing WASAPI audio input for  {device.DeviceInfo.Name}... ");
            inputDeviceIndex = device.WasapiDeviceIndex;
        }

        //Bass.Configure(Configuration.UpdateThreads, false);
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

        ActiveInputDeviceName = device.DeviceInfo.Name;
        var result = BassWasapi.Start();
        //Log.Debug("Wasapi.StartInputCapture() -> BassWasapi.Start():" + result);
    }
        
    private static void Stop()
    {
        //Log.Debug("Wasapi.Stop()");
        BassWasapi.Stop();
        BassWasapi.Free();
        ActiveInputDeviceName = null;
    }

    private static bool _complainedOnce;

        
    private static void InitializeInputDeviceList()
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
        //Log.Debug($"Wasapi.Process() called with buffer length: {length}");
        var level = BassWasapi.GetLevel();
        //if (length < 3000)
            //return length;

        _lastUpdateTime = Playback.RunTimeInSecs;

        int resultCode;
        if (_fftUpdatesSinceLastFrame == 0)
        {
            resultCode = BassWasapi.GetData(AudioAnalysis.FftGainBuffer, (int)(AudioAnalysis.BassFlagForFftBufferSize | DataFlags.FFTRemoveDC));
            //Log.Debug("Wasapi.Process() Result code after first update:" +resultCode);
        }
        else
        {
            resultCode = BassWasapi.GetData(_fftIntermediate, (int)(AudioAnalysis.BassFlagForFftBufferSize | DataFlags.FFTRemoveDC));
            //Log.Debug("Wasapi.Process() Result code after another update:" +resultCode);
            if (resultCode >= 0)
            {
                for (var i = 0; i < AudioAnalysis.FftHalfSize; i++)
                {
                    AudioAnalysis.FftGainBuffer[i] = MathF.Max(_fftIntermediate[i], AudioAnalysis.FftGainBuffer[i]);
                }
            }
        }

        _failedToGetLastFffData = resultCode < 0;
        if (_failedToGetLastFffData)
        {
            Log.Debug($"Can't get Wasapi FFT-Data: {Bass.LastError}");
        }

        _lastAudioLevel = (float)(level * 0.00001);
        _fftUpdatesSinceLastFrame++;
        //Log.Debug($"Process with {length} #{_fftUpdatesSinceLastFrame}  L:{audioLevel:0.0}  DevBufLen:{BassWasapi.Info.BufferLength}");
        return length;
    }

    private static int _fftUpdatesSinceLastFrame;
    private static bool _failedToGetLastFffData;

    public class WasapiInputDevice
    {
        public int WasapiDeviceIndex;
        public WasapiDeviceInfo DeviceInfo;
    }

    private static List<WasapiInputDevice> _inputDevices;
    private static readonly float[] _fftIntermediate = new float[AudioAnalysis.FftHalfSize];
    private static readonly WasapiProcedure _wasapiProcedure = Process;
    private static double _lastUpdateTime;

    public static string ActiveInputDeviceName { get; private set; }
    private static float _lastAudioLevel;
    public static float DecayingAudioLevel => (float)(_lastAudioLevel / Math.Max(1, (Playback.RunTimeInSecs - _lastUpdateTime) * 100));
}