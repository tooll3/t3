using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using T3.Core.Audio;
using T3.Core.Logging;

namespace T3.Editor.Gui.Audio
{
    public static class AudioImageFactory
    {
        public static string GetOrCreateImagePathForClip(AudioClip audioClip)
        {
            if (audioClip == null || !File.Exists(audioClip.FilePath))
            {
                return null;
            }
            
            if (ImageForAudioFiles.TryGetValue(audioClip.FilePath, out var imagePath))
            {
                return imagePath;
            }
            
            ImageForAudioFiles[audioClip.FilePath] = null; // Prevent double creation

            var task = new AsyncImageGeneratorTask(audioClip);
            task.Run();
            return null;
        }

        private class AsyncImageGeneratorTask
        {
            public AsyncImageGeneratorTask(AudioClip audioClip)
            {
                _audioClip = audioClip;
                _generator = new AudioImageGenerator(audioClip.FilePath);
            }

            public void Run()
            {
                Task.Run(GenerateAsync);
            }

            private void GenerateAsync()
            {
                Log.Debug($"Creating sound image for {_audioClip.FilePath}");
                var imageFilePath = _generator.GenerateSoundSpectrumAndVolume();
                if (imageFilePath == null)
                {
                    Log.Debug("could not create filepath");
                }
                else
                {
                    ImageForAudioFiles[_audioClip.FilePath] = imageFilePath;
                }
            }

            private AudioClip _audioClip;
            private readonly AudioImageGenerator _generator;
        }

        public static readonly Dictionary<string, string> ImageForAudioFiles = new();
    }
}