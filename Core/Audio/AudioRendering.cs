using System;
using System.Collections.Generic;
using System.Linq;
using ManagedBass;
using T3.Core.Animation;

namespace T3.Core.Audio;

public static class AudioRendering 
{
    public static void PrepareRecording(Playback playback, double fps)
    {
        _settingsBeforeExport.BassUpdateThreads = Bass.GetConfig(Configuration.UpdateThreads);
        _settingsBeforeExport.BassUpdatePeriodInMs = Bass.GetConfig(Configuration.UpdatePeriod);
        _settingsBeforeExport.BassGlobalStreamVolume = Bass.GetConfig(Configuration.GlobalStreamVolume);

        // Turn off automatic sound generation
        Bass.Pause();
        Bass.Configure(Configuration.UpdateThreads, false);
        Bass.Configure(Configuration.UpdatePeriod, 0);
        Bass.Configure(Configuration.GlobalStreamVolume, 0);
        
        foreach (var (_, clipStream) in AudioEngine.ClipStreams)
        {
            _settingsBeforeExport.BufferLengthInSeconds = Bass.ChannelGetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer);

            Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Volume, 1.0);
            Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer, 1.0 / fps);
            
            // TODO: Find this in Managed Bass library. It doesn't seem to be present.
            const int tailAttribute = 16;
            Bass.ChannelSetAttribute(clipStream.StreamHandle, (ChannelAttribute) tailAttribute, 2.0 / fps);
            Bass.ChannelStop(clipStream.StreamHandle);
            clipStream.UpdateTimeWhileRecording(playback, fps, true);
            Bass.ChannelPlay(clipStream.StreamHandle);
            Bass.ChannelPause(clipStream.StreamHandle);
        }

        _fifoBuffersForClips.Clear();
    }

        public static void ExportAudioFrame(Playback playback, double frameDurationInSeconds, AudioClipStream clipStream)
    {
        // Create buffer if necessary
        if (!_fifoBuffersForClips.TryGetValue(clipStream.ClipInfo, out var buffer)) 
        {
            buffer = _fifoBuffersForClips[clipStream.AudioClip] = new byte[0];
        }
        else
        {
            buffer = new byte[0]; // @Noice: This is a bug, right? 
        }

        // Update time position in clip
        var streamPositionInBytes = clipStream.UpdateTimeWhileRecording(playback, 1.0 / frameDurationInSeconds, true);

        var bytes = (int)Math.Max(Bass.ChannelSeconds2Bytes(clipStream.StreamHandle, frameDurationInSeconds), 0);
        if (buffer != null && bytes > 0)
        {
            while (buffer.Length < bytes)
            {
                // Add silence at the beginning of our buffer if necessary
                if (streamPositionInBytes < 0)
                {
                    // Clear the old buffer and replace with silence
                    _fifoBuffersForClips[clipStream.AudioClip] = new byte[0];
                    var silenceBytesToAdd = Math.Min(-streamPositionInBytes, bytes);
                    var silenceBuffer = new byte[silenceBytesToAdd];

                    // Append data to our previous buffer
                    buffer = buffer.Concat(silenceBuffer).ToArray();
                }

                if (buffer.Length < bytes)
                {
                    // Set the channel buffer size from here on
                    Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer,
                                             (int)Math.Round(frameDurationInSeconds * 1000.0));

                    // Update our own data
                    Bass.ChannelUpdate(clipStream.StreamHandle, (int)Math.Round(frameDurationInSeconds * 1000.0));
                }

                // Read all new data that is available
                var newBuffer = new byte[bytes];
                var newBytes = Bass.ChannelGetData(clipStream.StreamHandle, newBuffer, (int)DataFlags.Available);
                if (newBytes > 0)
                {
                    newBuffer = new byte[newBytes];
                    Bass.ChannelGetData(clipStream.StreamHandle, newBuffer, newBytes);

                    buffer = buffer.Concat(newBuffer).ToArray();

                    // Update the FFT now without reading more data
                    AudioEngine.UpdateFftBuffer(clipStream.StreamHandle, playback);
                }

                // Add silence at the end of our buffer if necessary
                if (buffer.Length < bytes)
                {
                    var silenceBytesToAdd = bytes - buffer.Length;
                    var silenceBuffer = new byte[silenceBytesToAdd];

                    // Append data to our previous buffer
                    buffer = buffer.Concat(silenceBuffer).ToArray();
                }
            }

            _fifoBuffersForClips[clipStream.AudioClip] = buffer;
        }

        // save to dictionary
        _fifoBuffersForClips[clipStream.ClipInfo] = buffer;
    }
    
    public static void EndRecording(Playback playback, double fps)
    {
        // TODO: Find this in Managed Bass library. It doesn't seem to be present.
        const int tailAttribute = 16;

        foreach (var (_, clipStream) in AudioEngine.ClipStreams)
        {
            // Bass.ChannelPause(clipStream.StreamHandle);
            clipStream.UpdateTimeWhileRecording(playback, fps, false);
            Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.NoRamp, 0);
            Bass.ChannelSetAttribute(clipStream.StreamHandle, (ChannelAttribute)tailAttribute, 0.0);
            Bass.ChannelSetAttribute(clipStream.StreamHandle, ChannelAttribute.Buffer, _settingsBeforeExport.BufferLengthInSeconds);
        }

        // restore live playback values
        Bass.Configure(Configuration.UpdatePeriod, _settingsBeforeExport.BassUpdatePeriodInMs);
        Bass.Configure(Configuration.GlobalStreamVolume, _settingsBeforeExport.BassGlobalStreamVolume);
        Bass.Configure(Configuration.UpdateThreads, _settingsBeforeExport.BassUpdateThreads);
        Bass.Start();
    }
    
    public static byte[] GetLastMixDownBuffer(double frameDurationInSeconds)
    {
        if (AudioEngine.ClipStreams.Count == 0)
        {
            // Get default sample rate
            var channels = AudioEngine.GetClipChannelCount(null);
            var sampleRate = AudioEngine.GetClipSampleRate(null);
            var samples = (int)Math.Max(Math.Round(frameDurationInSeconds * sampleRate), 0.0);
            var bytes = samples * channels * sizeof(float);

            return new byte[bytes];
        }

        foreach (var (_, clipStream) in AudioEngine.ClipStreams)
        {
            if (!_fifoBuffersForClips.TryGetValue(clipStream.ClipInfo, out var buffer))
                continue;
                    
            var bytes = (int)Bass.ChannelSeconds2Bytes(clipStream.StreamHandle, frameDurationInSeconds);
            var result = buffer.SkipLast(buffer.Length - bytes).ToArray();
            _fifoBuffersForClips[clipStream.ClipInfo] = buffer.Skip(bytes).ToArray();
            return result;
        }

        return null;
    }

    private static readonly Dictionary<AudioClipInfo, byte[]> _fifoBuffersForClips = new();

    private static BassSettingsBeforeExport _settingsBeforeExport;
    private struct BassSettingsBeforeExport
    {
        public int BassUpdatePeriodInMs ; // initial Bass library update period in MS
        public int BassGlobalStreamVolume ; // initial Bass library sample volume (range 0 to 10000)
        public int BassUpdateThreads; // initial Bass library update threads
        public double BufferLengthInSeconds; // FIXME: Why is that a single attribute for all clip streams?
    }

}