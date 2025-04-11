#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using T3.Core.Audio;
using T3.Core.Resource;

namespace T3.Editor.Gui.Audio;

internal static class AudioImageFactory
{
    // should be a hashset, but there is no ConcurrentHashset -_-
    private static readonly ConcurrentDictionary<AudioClipDefinition, bool> LoadingClips = new();

    internal static bool TryGetOrCreateImagePathForClip(AudioClipDefinition audioClip, IResourceConsumer? instance, [NotNullWhen(true)] out string? imagePath)
    {
        ArgumentNullException.ThrowIfNull(audioClip);
            
        if (LoadingClips.ContainsKey(audioClip))
        {
            imagePath = null;
            return false;
        }
            
        if (ImageForAudioFiles.TryGetValue(audioClip, out imagePath))
        {
            return true;
        }
            
        LoadingClips.TryAdd(audioClip, true);

        Task.Run(() =>
                 {
                     Log.Debug($"Creating sound image for {audioClip.FilePath}");
                     if (AudioImageGenerator.TryGenerateSoundSpectrumAndVolume(audioClip, instance, out var imagePath))
                     {
                         ImageForAudioFiles[audioClip] = imagePath;
                     }
                     else
                     {
                         Log.Error($"Failed to create sound image for {audioClip.FilePath}", instance);
                         ImageForAudioFiles.TryRemove(audioClip, out _);
                     }

                     LoadingClips.TryRemove(audioClip, out _);
                 });
            
        return false;
    }

    private static readonly ConcurrentDictionary<AudioClipDefinition, string> ImageForAudioFiles = new();
}