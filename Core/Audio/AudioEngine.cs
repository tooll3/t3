using System;
using System.Collections.Generic;
using System.IO;
using ManagedBass;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core;
using T3.Core.Animation;
using T3.Core.Logging;

namespace Core.Audio
{
    /// <summary>
    /// Controls loading, playback and discarding or audio clips.
    /// </summary>
    public static class AudioEngine
    {
        public static void UseAudioClip(AudioClip clip, double time)
        {
            _updatedClipTimes[clip] = time;
        }

        public static void CompleteFrame(Playback playback)
        {
            if (!_bassInitialized)
            {
                Bass.Free();
                Bass.Init();
                _bassInitialized = true;
            }
            
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
            foreach ( var (audioClipId,clipStream) in  _clipPlaybacks)
            {
                clipStream.IsInUse = _updatedClipTimes.ContainsKey(clipStream.AudioClip);
                if (!clipStream.IsInUse)
                {
                    obsoleteIds.Add(audioClipId);
                }
                else
                {
                    if (playbackSpeedChanged)
                        clipStream.UpdatePlaybackSpeed(playback.PlaybackSpeed);

                    if (!handledMainSoundtrack && clipStream.AudioClip.IsSoundtrack)
                    {
                        UpdateFftBuffer(clipStream.StreamHandle);
                        handledMainSoundtrack = true;
                    }
                    clipStream.UpdateTime();
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
            if (configAudioMuted)
            {
                _originalVolumeBeforeMuting = Bass.Volume;
            }
            Bass.Volume = configAudioMuted ? 0 : _originalVolumeBeforeMuting;
        }

        private static double _originalVolumeBeforeMuting;
        
        private static void UpdateFftBuffer(int soundStreamHandle)
        {
            const int get256FftValues = (int)DataFlags.FFT512;
            Bass.ChannelGetData(soundStreamHandle, FftBuffer, get256FftValues);
        }
        
        private static double _lastPlaybackSpeed = 1;
        private static bool _bassInitialized;
        private static readonly Dictionary<Guid, AudioClipStream> _clipPlaybacks = new();
        private static readonly Dictionary<AudioClip, double> _updatedClipTimes = new();
        
        private const int FftSize = 256;
        public static readonly float[] FftBuffer =  new float[FftSize];
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
            // Stop
            if (newSpeed == 0.0)
            {
                Bass.ChannelStop(StreamHandle);
            }
            // Play backwards
            else if (newSpeed < 0.0)
            {
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.ReverseDirection, -1);
                Bass.ChannelSetAttribute(StreamHandle, ChannelAttribute.Frequency, DefaultPlaybackFrequency * -newSpeed);
                Bass.ChannelPlay(StreamHandle);
            }
            else
            {
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
            var streamHandle = Bass.CreateStream(clip.FilePath, 0,0, BassFlags.Prescan);
            Bass.ChannelGetAttribute(streamHandle, ChannelAttribute.Frequency, out var defaultPlaybackFrequency);
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
        
        private const double AudioSyncingOffset = -2 / 60f;
        private const double AudioTriggerDelayOffset = 2 / 60f;
        private const double ResyncThreshold = 1.5f / 60f;
            
        /// <summary>
        /// We try to find a compromise between letting bass play the audio clip in the correct playback speed which
        /// eventually will drift away from the Playback time. Of the delta between playback and audio-clip time exceeds
        /// a threshold, we resync.
        /// Frequent resync causes audio glitches.
        /// Too large of a threshold can disrupt syncing.  
        /// </summary>
        public void UpdateTime()
        {
            if (Playback.Current.PlaybackSpeed == 0)
                return;

            var isOutOfBounds = TargetTime < AudioClip.StartTime || TargetTime >= AudioClip.LengthInSeconds + AudioClip.StartTime;
            var isPlaying = Bass.ChannelIsActive(StreamHandle) == PlaybackState.Playing;
            
            if (isOutOfBounds)
            {
                if(isPlaying)
                    Bass.ChannelPause(StreamHandle);
                
                return;
            }

            if (!isPlaying)
            {
                Bass.ChannelPlay(StreamHandle);
            }
            
            var currentStreamPos = Bass.ChannelGetPosition(StreamHandle);
            var currentPos = Bass.ChannelBytes2Seconds(StreamHandle, currentStreamPos) - AudioSyncingOffset;
            var soundDelta = currentPos - TargetTime;

            if (Math.Abs(soundDelta) <= ResyncThreshold * Math.Abs(Playback.Current.PlaybackSpeed)) 
                return;
            
            // Resync
            //Log.Debug($"Sound delta {soundDelta:0.000}s for {AudioClip.FilePath}");
            var newStreamPos = Bass.ChannelSeconds2Bytes(StreamHandle, TargetTime + AudioTriggerDelayOffset * Playback.Current.PlaybackSpeed + AudioSyncingOffset);
            Bass.ChannelSetPosition(StreamHandle, newStreamPos);

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