using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ManagedBass;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpDX.Win32;
using T3.Core.Animation;
using T3.Core.IO;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Resource;

namespace T3.Core.Audio
{
    /// <summary>
    /// Controls loading, playback and discarding of audio clips.
    /// </summary>
    public static class AudioEngine
    {
        public static int clipChannels(AudioClip clip)
        {
            if (clip != null && _clipPlaybacks.TryGetValue(clip.Id, out var stream))
            {
                Bass.ChannelGetInfo(stream.StreamHandle, out var info);
                return info.Channels;
            }

            // by default use stereo
            return 2;
        }
        public static int clipSampleRate(AudioClip clip)
        {
            if (clip != null && _clipPlaybacks.TryGetValue(clip.Id, out var stream))
            {
                Bass.ChannelGetInfo(stream.StreamHandle, out var info);
                return info.Frequency;
            }

            // return default sample rate (48000Hz)
            return 48000;
        }

        public static void UseAudioClip(AudioClip clip, double time)
        {
            _updatedClipTimes[clip] = time;
        }

        public static void ReloadClip(AudioClip clip)
        {
            if (_clipPlaybacks.TryGetValue(clip.Id, out var stream))
            {
                Bass.StreamFree(stream.StreamHandle);
                _clipPlaybacks.Remove(clip.Id);
            }
            
            UseAudioClip(clip,0);
        }

        public static void prepareRecording(Playback playback, double fps)
        {
            _bassUpdateThreads = Bass.GetConfig(Configuration.UpdateThreads);
            _bassUpdatePeriod = Bass.GetConfig(Configuration.UpdatePeriod);
            _bassGlobalStreamVolume = Bass.GetConfig(Configuration.GlobalStreamVolume);

            // turn off automatic sound generation
            Bass.Pause();
            Bass.Configure(Configuration.UpdateThreads, false);
            Bass.Configure(Configuration.UpdatePeriod, 0);
            Bass.Configure(Configuration.GlobalStreamVolume, 0);

            // TODO: Find this in Managed Bass library. It doesn't seem to be present.
            int tailAttribute = (int)16;

            foreach (var (audioClipId, clipStream) in _clipPlaybacks)
            {
                _oldBufferInSeconds = Bass.ChannelGetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer);

                Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Volume, 1.0);
                Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer, 1.0 / fps);
                Bass.ChannelSetAttribute(clipStream.StreamHandle, (ChannelAttribute) tailAttribute, 2.0 / fps);
                Bass.ChannelStop(clipStream.StreamHandle);
                clipStream.UpdateTimeRecord(playback, fps, true);
                Bass.ChannelPlay(clipStream.StreamHandle);
                Bass.ChannelPause(clipStream.StreamHandle);
            }

            _fifoBuffers.Clear();
        }

        public static void endRecording(Playback playback, double fps)
        {
            // TODO: Find this in Managed Bass library. It doesn't seem to be present.
            int tailAttribute = (int)16;

            foreach (var (audioClipId, clipStream) in _clipPlaybacks)
            {
                // Bass.ChannelPause(clipStream.StreamHandle);
                clipStream.UpdateTimeRecord(playback, fps, false);
                Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.NoRamp, 0);
                Bass.ChannelSetAttribute(clipStream.StreamHandle, (ChannelAttribute)tailAttribute, 0.0);
                Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer, _oldBufferInSeconds);
            }

            // restore live playback values
            Bass.Configure(Configuration.UpdatePeriod, _bassUpdatePeriod);
            Bass.Configure(Configuration.GlobalStreamVolume, _bassGlobalStreamVolume);
            Bass.Configure(Configuration.UpdateThreads, _bassUpdateThreads);
            Bass.Start();
        }

        public static void CompleteFrame(Playback playback,
            double frameDurationInSeconds)
        {
            if (!_bassInitialized)
            {
                Bass.Free();
                Bass.Init();
                _bassInitialized = true;
            }
            AudioAnalysis.CompleteFrame(playback);
            
            // Create new streams
            foreach (var (audioClip, time) in _updatedClipTimes)
            {
                if (_clipPlaybacks.TryGetValue(audioClip.Id, out var clip))
                {
                    clip.TargetTime = time;
                }
                else
                {
                    var audioClipStream = AudioClipStream.LoadClip(audioClip);
                    if (audioClipStream != null)
                        _clipPlaybacks[audioClip.Id] = audioClipStream;
                }
            }
            
            List<Guid> obsoleteIds = new();
            var playbackSpeedChanged = Math.Abs(_lastPlaybackSpeed - playback.PlaybackSpeed) > 0.001f;
            _lastPlaybackSpeed = playback.PlaybackSpeed;

            var handledMainSoundtrack = false;
            foreach (var (audioClipId,clipStream) in _clipPlaybacks)
            {
                clipStream.IsInUse = _updatedClipTimes.ContainsKey(clipStream.AudioClip);
                if (!clipStream.IsInUse)
                {
                    obsoleteIds.Add(audioClipId);
                }
                else
                {
                    if (playback.IsLive && playbackSpeedChanged)
                        clipStream.UpdatePlaybackSpeed(playback.PlaybackSpeed);

                    if (!handledMainSoundtrack && clipStream.AudioClip.IsSoundtrack)
                    {
                        if (!playback.IsLive)
                        {
                            // create buffer if necessary
                            byte[] buffer = null;
                            if (!_fifoBuffers.TryGetValue(clipStream.AudioClip, out buffer))
                                buffer = _fifoBuffers[clipStream.AudioClip] = new byte[0];
                            else
                                buffer = new byte[0];

                            // update time position in clip
                            var streamPositionInBytes = clipStream.UpdateTimeRecord(playback, 1.0 / frameDurationInSeconds, true);

                            var bytes = (int)Math.Max(Bass.ChannelSeconds2Bytes(clipStream.StreamHandle, frameDurationInSeconds), 0);
                            if (buffer != null && bytes > 0)
                            {
                                while (buffer.Length < bytes)
                                {
                                    // add silence at the beginning of our buffer if necessary
                                    if (streamPositionInBytes < 0)
                                    {
                                        // clear the old buffer and replace with silence
                                        _fifoBuffers[clipStream.AudioClip] = new byte[0];
                                        var silenceBytesToAdd = Math.Min(-streamPositionInBytes, bytes);
                                        var silenceBuffer = new byte[silenceBytesToAdd];
                                        // append data to our previous buffer
                                        buffer = buffer.Concat(silenceBuffer).ToArray();
                                    }

                                    if (buffer.Length < bytes)
                                    {
                                        // set the channel buffer size from here on
                                        Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer,
                                            (int)Math.Round(frameDurationInSeconds * 1000.0));

                                        // update our own data
                                        Bass.ChannelUpdate(clipStream.StreamHandle, (int)Math.Round(frameDurationInSeconds * 1000.0));
                                    }

                                    // read all new data that is available
                                    var newBuffer = new byte[bytes];
                                    //Bass.ChannelStop(clipStream.StreamHandle);
                                    //Bass.ChannelPause(clipStream.StreamHandle);
                                    var newBytes = Bass.ChannelGetData(clipStream.StreamHandle, newBuffer, (int)DataFlags.Available);
                                    if (newBytes > 0)
                                    {
                                        newBuffer = new byte[newBytes];
                                        Bass.ChannelGetData(clipStream.StreamHandle, newBuffer, newBytes);
                                        // use number of available bytes to write the data into a new array
                                        //var soundBytesToAdd = Math.Min(newBytes, bytes - buffer.Length);
                                        // append valid data to our previous buffer
                                        //buffer = buffer.Concat(newBuffer.Take(soundBytesToAdd)).ToArray();

                                        buffer = buffer.Concat(newBuffer).ToArray();

                                        // update the FFT now without reading more data
                                        UpdateFftBuffer(clipStream.StreamHandle, playback);
                                    }

                                    // add silence at the end of our buffer if necessary
                                    if (buffer.Length < bytes)
                                    {
                                        var silenceBytesToAdd = bytes - buffer.Length;
                                        var silenceBuffer = new byte[silenceBytesToAdd];
                                        // append data to our previous buffer
                                        buffer = buffer.Concat(silenceBuffer).ToArray();
                                    }
                                }

                                _fifoBuffers[clipStream.AudioClip] = buffer;
                                handledMainSoundtrack = true;
                            }
                        }
                        else
                        {
                            UpdateFftBuffer(clipStream.StreamHandle, playback);
                            clipStream.UpdateTimeLive(playback);
                            handledMainSoundtrack = true;
                        }
                    }
                }
            }

            foreach(var id in obsoleteIds)
            {
                _clipPlaybacks[id].Disable();
                _clipPlaybacks.Remove(id);
            }
            _updatedClipTimes.Clear();
        }

        public static void SetMute(bool configAudioMuted)
        {
            IsMuted = configAudioMuted;
            UpdateMuting();
        }
        
        public static bool IsMuted { get; private set; }
        
        private static void UpdateMuting()
        {
            foreach (var stream in _clipPlaybacks.Values)
            {
                var volume = IsMuted ? 0 : 1;
                Bass.ChannelSetAttribute(stream.StreamHandle,ChannelAttribute.Volume, volume);
            }
        }

        private static void UpdateFftBuffer(int soundStreamHandle, Playback playback)
        {
            int get256FftValues = (int)DataFlags.FFT512;

            // do not advance plaback if we are not in live mode
            if (!playback.IsLive)
                get256FftValues |= (int)268435456; // TODO: find BASS_DATA_NOREMOVE in ManagedBass

            if (playback.Settings != null && playback.Settings.AudioSource == PlaybackSettings.AudioSources.ProjectSoundTrack)
            {
                Bass.ChannelGetData(soundStreamHandle, AudioAnalysis.FftGainBuffer, get256FftValues);
            }
        }

        public static byte[] LastMixDownBuffer(double frameDurationInSeconds)
        {
            if (_clipPlaybacks.Count == 0)
            {
                // get default sample rate
                var channels = clipChannels(null);
                var sampleRate = clipSampleRate(null);
                var samples = (int)Math.Max(Math.Round(frameDurationInSeconds * sampleRate), 0.0);
                var bytes = samples * channels * sizeof(float);

                return new byte[bytes];
            }
            else
            {
                foreach (var (audioClipId, clipStream) in _clipPlaybacks)
                {
                    if (_fifoBuffers.TryGetValue(clipStream.AudioClip, out var buffer))
                    {
                        var bytes = (int)Bass.ChannelSeconds2Bytes(clipStream.StreamHandle, frameDurationInSeconds);

                        var result = buffer.SkipLast(buffer.Length - bytes).ToArray();
                        _fifoBuffers[clipStream.AudioClip] = buffer.Skip(bytes).ToArray();

                        return result;
                    }
                }
            }

            // error
            return null;
        }

        private static double _lastPlaybackSpeed = 1;
        private static bool _bassInitialized;
        private static double _oldBufferInSeconds;
        private static readonly Dictionary<Guid, AudioClipStream> _clipPlaybacks = new();
        private static readonly Dictionary<AudioClip, double> _updatedClipTimes = new();
        private static readonly Dictionary<AudioClip, byte[]> _fifoBuffers = new();

        // to save bass state before recording
        private static int _bassUpdatePeriod; // initial Bass library update period in MS
        private static int _bassGlobalStreamVolume; // initial Bass library sample volume (range 0 to 10000)
        private static int _bassUpdateThreads; // initial Bass library update threads
    }


    public class AudioClipStream
    {
        public AudioClip AudioClip;
        
        public double Duration;
        public int StreamHandle;
        public bool IsInUse;
        public bool IsNew = true;
        public float DefaultPlaybackFrequency { get; private set; }
        public double TargetTime { get; set; }

        public void UpdatePlaybackSpeed(double newSpeed)
        {
            if (newSpeed == 0.0)
            {
                // Stop
                Bass.ChannelStop(StreamHandle);
            }
            else if (newSpeed < 0.0)
            {
                // Play backwards
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.ReverseDirection, -1);
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.Frequency, DefaultPlaybackFrequency * -newSpeed);
                Bass.ChannelPlay(StreamHandle);
            }
            else
            {
                // Play forward
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.ReverseDirection, 1);
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.Frequency, DefaultPlaybackFrequency * newSpeed);
                Bass.ChannelPlay(StreamHandle);
            }
        }


        public static AudioClipStream LoadClip(AudioClip clip)
        {
            if (string.IsNullOrEmpty(clip.FilePath))
                return null;
            
            Log.Debug($"Loading audioClip {clip.FilePath} ...");
            if (!File.Exists(clip.FilePath))
            {
                Log.Error($"AudioClip file '{clip.FilePath}' does not exist.");
                return null;
            }
            var streamHandle = Bass.CreateStream(clip.FilePath, 0, 0, BassFlags.Prescan | BassFlags.Float);
            Bass.ChannelGetAttribute(streamHandle, ChannelAttribute.Frequency, out var defaultPlaybackFrequency);
            Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, AudioEngine.IsMuted ? 0 : 1);
            var bytes = Bass.ChannelGetLength(streamHandle);
            if (bytes < 0)
            {
                Log.Error($"Failed to initialize audio playback for {clip.FilePath}.");
            }
            var duration = (float)Bass.ChannelBytes2Seconds(streamHandle, bytes);
            
            var stream = new AudioClipStream()
                             {
                                 AudioClip = clip,
                                 StreamHandle = streamHandle,
                                 DefaultPlaybackFrequency = defaultPlaybackFrequency,
                                 Duration = duration,
                             };

            clip.LengthInSeconds = duration;
            return stream;
        }

        private const double AudioSyncingOffset = -2.0 / 60.0;
        private const double AudioTriggerDelayOffset = 2.0 / 60.0;
        private const double RecordSyncingOffset = -1.0 / 60.0;

        /// <summary>
        /// We try to find a compromise between letting bass play the audio clip in the correct playback speed which
        /// eventually will drift away from Tooll's Playback time. If the delta between playback and audio-clip time exceeds
        /// a threshold, we resync.
        /// Frequent resync causes audio glitches.
        /// Too large of a threshold can disrupt syncing and increase latency.
        /// </summary>
        /// <param name="playback"></param>
        public void UpdateTimeLive(Playback playback)
        {
            if (playback.PlaybackSpeed == 0)
            {
                Bass.ChannelPause(StreamHandle);
                return;
            }
            
            var localTargetTimeInSecs = TargetTime - playback.SecondsFromBars(AudioClip.StartTime);
            var isOutOfBounds = localTargetTimeInSecs < 0 || localTargetTimeInSecs >= AudioClip.LengthInSeconds;
            var isPlaying = Bass.ChannelIsActive(StreamHandle) == PlaybackState.Playing;
            
            if (isOutOfBounds)
            {
                if (isPlaying)
                {
                    //Log.Debug("Pausing");
                    Bass.ChannelPause(StreamHandle);
                }
                return;
            }

            if (!isPlaying)
            {
                //Log.Debug("Restarting");
                Bass.ChannelPlay(StreamHandle);
            }

            var currentStreamPos = Bass.ChannelGetPosition(StreamHandle);
            var currentPos = Bass.ChannelBytes2Seconds(StreamHandle, currentStreamPos) - AudioSyncingOffset;
            var soundDelta = (currentPos - localTargetTimeInSecs) * playback.PlaybackSpeed;

            // we may not fall behind or skip ahead in playback
            var maxSoundDelta = ProjectSettings.Config.AudioResyncThreshold * Math.Abs(playback.PlaybackSpeed);
            if (Math.Abs(soundDelta) <= maxSoundDelta)
                return;

            // Resync
            //Log.Debug($"Sound delta {soundDelta:0.000}s for {AudioClip.FilePath}");
            var resyncOffset = AudioTriggerDelayOffset * playback.PlaybackSpeed + AudioSyncingOffset;
            var newStreamPos = Bass.ChannelSeconds2Bytes(StreamHandle, localTargetTimeInSecs + resyncOffset);
            Bass.ChannelSetPosition(StreamHandle, newStreamPos, PositionFlags.Bytes);
        }

        /// <summary>
        /// Update time when recoding, returns number of bytes of the position from the stream start
        /// </summary>
        /// <param name="playback"></param>
        public long UpdateTimeRecord(Playback playback, double fps, bool reinitialize)
        {
            // offset timing dependent on position in clip
            var localTargetTimeInSecs = playback.TimeInSecs - playback.SecondsFromBars(AudioClip.StartTime) + RecordSyncingOffset;
            long newStreamPos = 0;
            if (localTargetTimeInSecs < 0)
                newStreamPos = -Bass.ChannelSeconds2Bytes(StreamHandle, -localTargetTimeInSecs);
            else
                newStreamPos = Bass.ChannelSeconds2Bytes(StreamHandle, localTargetTimeInSecs);

            // re-initialize playback?
            if (reinitialize)
            {
                var flags = PositionFlags.Bytes | PositionFlags.MixerNoRampIn | PositionFlags.Decode;

                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.NoRamp, 1);
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.Volume, 1);
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.ReverseDirection, 1);
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.Frequency, DefaultPlaybackFrequency);
                Bass.ChannelSetPosition(StreamHandle, Math.Max(newStreamPos, 0), flags);
            }

            return newStreamPos;
        }

        public void Disable()
        {
            Bass.StreamFree(StreamHandle);
        }
    }
    
    public class AudioClip
    {
        #region serialized attributes 
        public Guid Id;
        public string FilePath;
        public double StartTime;
        public double EndTime;
        public float Bpm = 120;
        public bool DiscardAfterUse = true;
        public bool IsSoundtrack = false;
        #endregion

        /// <summary>
        /// Is initialized after loading...
        /// </summary>
        public double LengthInSeconds;  

        
        #region serialization
        public static AudioClip FromJson(JToken jToken)
        {
            var idToken = jToken[nameof(Id)];

            var idString = idToken?.Value<string>();
            if (idString == null)
                return null;

            var newAudioClip = new AudioClip
                                   {
                                       Id = Guid.Parse(idString),
                                       FilePath = jToken[nameof(FilePath)]?.Value<string>() ?? String.Empty,
                                       StartTime = jToken[nameof(StartTime)]?.Value<double>() ?? 0,
                                       EndTime = jToken[nameof(EndTime)]?.Value<double>() ?? 0,
                                       Bpm = jToken[nameof(Bpm)]?.Value<float>() ?? 0,
                                       DiscardAfterUse = jToken[nameof(DiscardAfterUse)]?.Value<bool>() ?? true,
                                       IsSoundtrack = jToken[nameof(IsSoundtrack)]?.Value<bool>() ?? true,
                                   };
            
            return newAudioClip;
        }

        public void ToJson(JsonTextWriter writer)
        {
            //writer.WritePropertyName(Id.ToString());
            writer.WriteStartObject();
            {
                writer.WriteValue(nameof(Id), Id);
                writer.WriteValue(nameof(StartTime), StartTime);
                writer.WriteValue(nameof(EndTime), EndTime);
                writer.WriteValue(nameof(Bpm), Bpm);
                writer.WriteValue(nameof(DiscardAfterUse), DiscardAfterUse);
                writer.WriteValue(nameof(IsSoundtrack), IsSoundtrack);
                writer.WriteObject(nameof(FilePath), FilePath);
            }
            writer.WriteEndObject();
        }

        #endregion        
    }
}