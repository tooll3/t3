using System;
using System.IO;
using ManagedBass;
using T3.Core.Animation;
using T3.Core.IO;
using T3.Core.Logging;

namespace T3.Core.Audio;

/// <summary>
/// Controls the playback of a <see cref="AudioClip"/> with BASS.
/// </summary>
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
        Bass.ChannelPlay(streamHandle);
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

        stream.UpdatePlaybackSpeed(1.0);
        return stream;
    }


    /// <summary>
    /// We try to find a compromise between letting bass play the audio clip in the correct playback speed which
    /// eventually will drift away from Tooll's Playback time. If the delta between playback and audio-clip time exceeds
    /// a threshold, we resync.
    /// 
    /// Frequent resync causes audio glitches.
    /// Too large of a threshold can disrupt syncing and increase latency.
    /// </summary>
    public void UpdateTime(Playback playback)
    {
        if (playback.PlaybackSpeed == 0 )
        {
            Bass.ChannelPause(StreamHandle);
            return;
        }

        var localTargetTimeInSecs = TargetTime - playback.SecondsFromBars(AudioClip.StartTime);
        var isOutOfBounds = localTargetTimeInSecs < 0 || localTargetTimeInSecs >= AudioClip.LengthInSeconds;
        var channelIsActive = Bass.ChannelIsActive(StreamHandle);
        var isPlaying = channelIsActive == PlaybackState.Playing; // || channelIsActive == PlaybackState.Stalled;

        // if (channelIsActive == PlaybackState.Stalled)
        //     Log.Debug(" Stalling?");
        
        
        if (isOutOfBounds)
        {
            if (isPlaying)
            {
                Bass.ChannelPause(StreamHandle);
            }

            return;
        }

        if (!isPlaying)
        {
            Bass.ChannelPlay(StreamHandle);
        }

        var currentStreamBufferPos = Bass.ChannelGetPosition(StreamHandle);
        var currentPosInSec = Bass.ChannelBytes2Seconds(StreamHandle, currentStreamBufferPos) - AudioSyncingOffset;
        var soundDelta = (currentPosInSec - localTargetTimeInSecs) * playback.PlaybackSpeed;
            
        // We may not fall behind or skip ahead in playback
        var maxSoundDelta = ProjectSettings.Config.AudioResyncThreshold * Math.Abs(playback.PlaybackSpeed);
        if (Math.Abs(soundDelta) <= maxSoundDelta )
            return;

        //Log.Debug($" Resyncing audio playback. target:{localTargetTimeInSecs:0.00}s  {currentPosInSec:0.00} {soundDelta:0.00} > {maxSoundDelta:0.00}");
        
        // Resync
        var resyncOffset = AudioTriggerDelayOffset * playback.PlaybackSpeed + AudioSyncingOffset;
        var newStreamPos = Bass.ChannelSeconds2Bytes(StreamHandle, localTargetTimeInSecs + resyncOffset);
        Bass.ChannelSetPosition(StreamHandle, newStreamPos, PositionFlags.Bytes);
    }

    /// <summary>
    /// Update time when recoding, returns number of bytes of the position from the stream start
    /// </summary>
    public long UpdateTimeWhileRecording(Playback playback, double fps, bool reinitialize)
    {
        // Offset timing dependent on position in clip
        var localTargetTimeInSecs = playback.TimeInSecs - playback.SecondsFromBars(AudioClip.StartTime) + RecordSyncingOffset;
        var newStreamPos = localTargetTimeInSecs < 0
                               ? -Bass.ChannelSeconds2Bytes(StreamHandle, -localTargetTimeInSecs)
                               : Bass.ChannelSeconds2Bytes(StreamHandle, localTargetTimeInSecs);

        // Re-initialize playback?
        if (reinitialize)
        {
            const PositionFlags flags = PositionFlags.Bytes | PositionFlags.MixerNoRampIn | PositionFlags.Decode;

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
        
        
    private const double AudioSyncingOffset = -2.0 / 60.0;
    private const double AudioTriggerDelayOffset = 2.0 / 60.0;
    private const double RecordSyncingOffset = -1.0 / 60.0;
}