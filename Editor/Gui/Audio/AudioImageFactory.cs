using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using T3.Core.Audio;
using T3.Core.Logging;

namespace T3.Editor.Gui.Audio
{
    public static class AudioImageFactory
    {
        private static readonly ConcurrentDictionary<AudioClip, bool> LoadingClips = new();
        public static string GetOrCreateImagePathForClip(AudioClip audioClip)
        {
            if (audioClip == null || LoadingClips.ContainsKey(audioClip) || !audioClip.TryGetAbsoluteFilePath(out var absolutePath))
            {
                return null;
            }
            
            if (ImageForAudioFiles.TryGetValue(absolutePath, out var imagePath))
            {
                return imagePath;
            }
            
            LoadingClips.TryAdd(audioClip, true);
            
            Task.Run(() =>
                     {
                         var generator = new AsyncImageGeneratorTask(audioClip);
                         generator.Generate();
                         LoadingClips.TryRemove(audioClip, out _);
                     });
            return null;
        }

        private class AsyncImageGeneratorTask
        {
            public AsyncImageGeneratorTask(AudioClip audioClip)
            {
                _audioClip = audioClip;
                _generator = new AudioImageGenerator(audioClip);
            }

            public void Generate()
            {
                Log.Debug($"Creating sound image for {_audioClip.FilePath}");
                if (!_generator.TryGenerateSoundSpectrumAndVolume())
                {
                    Log.Debug("could not create filepath");
                }
                else
                {
                    ImageForAudioFiles[_generator.SoundFilePathAbsolute] = _generator.ImageFilePathAbsolute;
                }
            }

            private readonly AudioClip _audioClip;
            private readonly AudioImageGenerator _generator;
        }

        public static readonly Dictionary<string, string> ImageForAudioFiles = new();
    }
}