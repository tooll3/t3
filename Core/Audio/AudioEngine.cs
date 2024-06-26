using System;
using System.Collections.Generic;
using ManagedBass;
using T3.Core.Animation;
using T3.Core.Operator;

namespace T3.Core.Audio
{
    /// <summary>
    /// Controls loading, playback and discarding of audio clips.
    /// </summary>
    public static class AudioEngine
    {
        public static void UseAudioClip(AudioClipInfo info, double time)
        {
            _updatedClipTimes[info] = time;
        }

        public static void ReloadClip(AudioClipInfo info)
        {
            if (ClipStreams.TryGetValue(info, out var stream))
            {
                Bass.StreamFree(stream.StreamHandle);
                ClipStreams.Remove(info);
            }

            UseAudioClip(info, 0);
        }

        public static void CompleteFrame(Playback playback, double frameDurationInSeconds)
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
                if (ClipStreams.TryGetValue(audioClip, out var clip))
                {
                    clip.TargetTime = time;
                }
                else
                {
                    var audioClipStream = AudioClipStream.LoadClip(audioClip);
                    if (audioClipStream != null)
                        ClipStreams[audioClip] = audioClipStream;
                }
            }

            List<AudioClipInfo> obsoleteIds = new();
            var playbackSpeedChanged = Math.Abs(_lastPlaybackSpeed - playback.PlaybackSpeed) > 0.001f;
            _lastPlaybackSpeed = playback.PlaybackSpeed;

            var handledMainSoundtrack = false;
            foreach (var (audioClipId, clipStream) in ClipStreams)
            {
                clipStream.IsInUse = _updatedClipTimes.ContainsKey(clipStream.ClipInfo);
                if (!clipStream.IsInUse && clipStream.ClipInfo.Clip.DiscardAfterUse)
                {
                    obsoleteIds.Add(audioClipId);
                }
                else
                {
                    if (!playback.IsRenderingToFile && playbackSpeedChanged)
                        clipStream.UpdatePlaybackSpeed(playback.PlaybackSpeed);

                    if (handledMainSoundtrack || !clipStream.ClipInfo.Clip.IsSoundtrack)
                        continue;

                    handledMainSoundtrack = true;

                    if (playback.IsRenderingToFile)
                    {
                        AudioRendering.ExportAudioFrame(playback, frameDurationInSeconds, clipStream);
                    }
                    else
                    {
                        UpdateFftBuffer(clipStream.StreamHandle, playback);
                        clipStream.UpdateTime(playback);
                    }
                }
            }

            foreach (var id in obsoleteIds)
            {
                ClipStreams[id].Disable();
                ClipStreams.Remove(id);
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
            foreach (var stream in ClipStreams.Values)
            {
                var volume = IsMuted ? 0 : 1;
                Bass.ChannelSetAttribute(stream.StreamHandle, ChannelAttribute.Volume, volume);
            }
        }

        public static void UpdateFftBuffer(int soundStreamHandle, Playback playback)
        {
            // FIXME: This variable name is misleading or incorrect
            var get256FftValues = (int)DataFlags.FFT2048;

            // Do not advance playback if we are not in live mode
            if (playback.IsRenderingToFile)
                get256FftValues |= (int)268435456; // TODO: find BASS_DATA_NOREMOVE in ManagedBass

            if (playback.Settings != null && playback.Settings.AudioSource == PlaybackSettings.AudioSources.ProjectSoundTrack)
            {
                Bass.ChannelGetData(soundStreamHandle, AudioAnalysis.FftGainBuffer, get256FftValues);
            }
        }

        public static int GetClipChannelCount(AudioClipInfo? clip)
        {
            // By default use stereo
            if (clip == null || !ClipStreams.TryGetValue(clip.Value, out var clipStream))
                return 2;

            Bass.ChannelGetInfo(clipStream.StreamHandle, out var info);
            return info.Channels;
        }

        // TODO: Rename to GetClipOrDefaultSampleRate
        public static int GetClipSampleRate(AudioClipInfo? clip)
        {
            if (clip == null || !ClipStreams.TryGetValue(clip.Value, out var stream))
                return 48000;

            Bass.ChannelGetInfo(stream.StreamHandle, out var info);
            return info.Frequency;
        }

        private static double _lastPlaybackSpeed = 1;
        private static bool _bassInitialized;
        public static readonly Dictionary<AudioClipInfo, AudioClipStream> ClipStreams = new();
        private static readonly Dictionary<AudioClipInfo, double> _updatedClipTimes = new();

    }
}