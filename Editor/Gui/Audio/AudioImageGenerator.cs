using System;
using System.Drawing;
using System.IO;
using ManagedBass;
using Newtonsoft.Json;
using T3.Core.Logging;
using T3.Core.Utils;

namespace T3.Editor.Gui.Audio
{
    public class AudioImageGenerator
    {
        public AudioImageGenerator(string soundFilePath)
        {
            _soundFilePath = soundFilePath;
        }

        public string GenerateSoundSpectrumAndVolume()
        {
            try
            {
                if (string.IsNullOrEmpty(_soundFilePath) || !File.Exists(_soundFilePath))
                    return null;

                if (File.Exists(ImageFilePath))
                {
                    Log.Debug($"Reusing sound image file: {ImageFilePath}");
                    return ImageFilePath;
                }
            }
            catch(Exception e)
            {
                Log.Warning($"Failed to generated image for soundtrack {_soundFilePath}: " + e.Message);
                return null;
            }

            Log.Debug($"Generating {ImageFilePath}...");

            Bass.Init(-1, 44100, 0, IntPtr.Zero);
            var stream = Bass.CreateStream(_soundFilePath, 0, 0, BassFlags.Decode | BassFlags.Prescan);

            var streamLength = Bass.ChannelGetLength(stream);

            const double samplingResolution = 1.0 / 100;

            var sampleLength = Bass.ChannelSeconds2Bytes(stream, samplingResolution);
            var numSamples = streamLength / sampleLength;

            const int maxSamples = 16384;
            if (numSamples > maxSamples)
            {
                sampleLength = (long)(sampleLength * numSamples / (double)maxSamples) + 100;
                numSamples = streamLength / sampleLength;
                Log.Debug($"Limitting texture size to {numSamples} samples");
            }

            Bass.ChannelPlay(stream);

            var spectrumImage = new Bitmap((int)numSamples, ImageHeight);

            int a, b, r, g;
            var palette = new System.Drawing.Color[PaletteSize];
            int palettePos;

            for (palettePos = 0; palettePos < PaletteSize; ++palettePos)
            {
                a = 255;
                if (palettePos < PaletteSize * 0.666f)
                    a = (int)(palettePos * 255 / (PaletteSize * 0.666f));

                b = 0;
                if (palettePos < PaletteSize * 0.333f)
                    b = palettePos;
                else if (palettePos < PaletteSize * 0.666f)
                    b = -palettePos + 510;

                r = 0;
                if (palettePos > PaletteSize * 0.666f)
                    r = 255;
                else if (palettePos > PaletteSize * 0.333f)
                    r = palettePos - 255;

                g = 0;
                if (palettePos > PaletteSize * 0.666f)
                    g = palettePos - 510;

                palette[palettePos] = System.Drawing.Color.FromArgb(a, r, g, b);
            }

            foreach (var region in _regions)
            {
                region.Levels = new float[numSamples];
            }

            var f = (float)(SpectrumLength / Math.Log(ImageHeight + 1));
            var f2 = (float)((PaletteSize - 1) / Math.Log(MaxIntensity + 1));
            //var f3 = (float)((ImageHeight - 1) / Math.Log(32768.0f + 1));

            for (var sampleIndex = 0; sampleIndex < numSamples; ++sampleIndex)
            {
                Bass.ChannelSetPosition(stream, sampleIndex * sampleLength);
                Bass.ChannelGetData(stream, _fftBuffer, (int)DataFlags.FFT2048);

                for (var rowIndex = 0; rowIndex < ImageHeight; ++rowIndex)
                {
                    var j = (int)(f * Math.Log(rowIndex + 1));
                    var pj = (int)(rowIndex > 0 ? f * Math.Log(rowIndex - 1 + 1) : j);
                    var nj = (int)(rowIndex < ImageHeight - 1 ? f * Math.Log(rowIndex + 1 + 1) : j);
                    var intensity = 125.0f * _fftBuffer[SpectrumLength - pj - 1] +
                                    750.0f * _fftBuffer[SpectrumLength - j - 1] +
                                    125.0f * _fftBuffer[SpectrumLength - nj - 1];
                    intensity = Math.Min(MaxIntensity, intensity);
                    intensity = Math.Max(0.0f, intensity);

                    palettePos = (int)(f2 * Math.Log(intensity + 1));
                    spectrumImage.SetPixel(sampleIndex, rowIndex, palette[palettePos]);
                }

                if (sampleIndex % 1000 == 0)
                {
                    var percentage = (int)(100.0 * sampleIndex / (float)numSamples);
                    Log.Debug($"   computing sound image {percentage}%% complete");
                }

                // foreach (var region in _regions)
                // {
                //     region.ComputeUpLevelForCurrentFft(sampleIndex, ref _fftBuffer);
                // }
            }

            // foreach (var region in _regions)
            // {
            //     region.SaveToFile(_soundFilePath);
            // }

            spectrumImage.Save(ImageFilePath);
            Bass.ChannelStop(stream);
            Bass.StreamFree(stream);

            return ImageFilePath;
        }

        private class FftRegion
        {
            public string Title;
            public float[] Levels;
            public float LowerLimit;
            public float UpperLimit;

            public void ComputeUpLevelForCurrentFft(int index, ref float[] fftBuffer)
            {
                var level = 0f;

                var startIndex = (int)MathUtils.Lerp(0, SpectrumLength, MathUtils.Clamp(this.LowerLimit, 0, 1));
                var endIndex = (int)MathUtils.Lerp(0, SpectrumLength, MathUtils.Lerp(this.UpperLimit, 0, 1));

                for (int i = startIndex; i < endIndex; i++)
                {
                    level += fftBuffer[i];
                }

                Levels[index] = level;
            }

            public void SaveToFile(string basePath)
            {
                using (var sw = new StreamWriter(basePath + "." + Title + ".json"))
                {
                    sw.Write(JsonConvert.SerializeObject(Levels, Formatting.Indented));
                }
            }
        }

        private readonly string _soundFilePath;
        private string ImageFilePath => _soundFilePath + ".waveform.png";

        private readonly FftRegion[] _regions =
            {
                new() { Title = "levels", LowerLimit = 0f, UpperLimit = 1f },
                new() { Title = "highlevels", LowerLimit = 0.3f, UpperLimit = 1f },
                new() { Title = "midlevels", LowerLimit = 0.06f, UpperLimit = 0.3f },
                new() { Title = "lowlevels", LowerLimit = 0.0f, UpperLimit = 0.02f },
            };

        private const int SpectrumLength = 1024;
        private const int ImageHeight = 256;
        private const float MaxIntensity = 500;
        private const int ColorSteps = 255;
        private const int PaletteSize = 3 * ColorSteps;

        private float[] _fftBuffer = new float[SpectrumLength];
    }
}