using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.IO;
using SharpDX.WIC;
using T3.Core;
using T3.Core.Logging;

namespace T3.Gui.Windows
{
    public static class ScreenshotWriter
    {
        public enum FileFormats
        {
            Png,
            Jpg,
        }

        public static bool SaveBufferToFile(Texture2D texture2d, string filepath, FileFormats format)
        {
            if (texture2d == null)
                return false;

            var device = ResourceManager.Device;
            var currentDesc = texture2d.Description;
            if (ImagesWithCpuAccess.Count == 0
                || ImagesWithCpuAccess[0].Description.Format != currentDesc.Format
                || ImagesWithCpuAccess[0].Description.Width != currentDesc.Width
                || ImagesWithCpuAccess[0].Description.Height != currentDesc.Height
                || ImagesWithCpuAccess[0].Description.MipLevels != currentDesc.MipLevels)
            {
                Dispose();
                
                var imageDesc = new Texture2DDescription
                                    {
                                        BindFlags = BindFlags.None,
                                        Format = currentDesc.Format,
                                        Width = currentDesc.Width,
                                        Height = currentDesc.Height,
                                        MipLevels = currentDesc.MipLevels,
                                        SampleDescription = new SampleDescription(1, 0),
                                        Usage = ResourceUsage.Staging,
                                        OptionFlags = ResourceOptionFlags.None,
                                        CpuAccessFlags = CpuAccessFlags.Read,
                                        ArraySize = 1
                                    };
                
                for (int i = 0; i < NumTextureEntries; ++i)
                {
                    ImagesWithCpuAccess.Add(new Texture2D(device, imageDesc));
                }
                _currentIndex = 0;
                // skip the first two frames since they will only appear
                // after buffers have been swapped
                _currentUsageIndex = -SkipImages;
            }

            // copy the original texture to a readable image
            var immediateContext = device.ImmediateContext;
            var readableImage = ImagesWithCpuAccess[_currentIndex];
            immediateContext.CopyResource(texture2d, readableImage);
            immediateContext.UnmapSubresource(readableImage, 0);
            _currentIndex = (_currentIndex + 1) % NumTextureEntries;

            // don't return first two samples since buffering is not ready yet
            if (_currentUsageIndex++ < 0)
                return true;

            DataBox dataBox = immediateContext.MapSubresource(readableImage,
                                                                0,
                                                                0,
                                                                MapMode.Read,
                                                                SharpDX.Direct3D11.MapFlags.None,
                                                                out var imageStream);
            using (imageStream)
            {
                int width = currentDesc.Width;
                int height = currentDesc.Height;
                var factory = new ImagingFactory();

                WICStream stream = null;

                stream = new WICStream(factory, filepath, NativeFileAccess.Write);

                // Initialize a Jpeg encoder with this stream
                //var encoder = new PngBitmapEncoder(factory);
                //var encoder = new JpegBitmapEncoder(factory);
                BitmapEncoder encoder = (format == FileFormats.Png)
                                    ? new PngBitmapEncoder(factory)
                                    : new JpegBitmapEncoder(factory);
                encoder.Initialize(stream);

                // Create a Frame encoder
                var bitmapFrameEncode = new BitmapFrameEncode(encoder);
                bitmapFrameEncode.Initialize();
                bitmapFrameEncode.SetSize(width, height);
                var formatId = PixelFormat.Format32bppRGBA;
                bitmapFrameEncode.SetPixelFormat(ref formatId);

                // Write a pseudo-plasma to a buffer
                int rowStride = PixelFormat.GetStride(formatId, width);
                var outBufferSize = height * rowStride;
                var outDataStream = new DataStream(outBufferSize, true, true);
                var pixelByteCount = PixelFormat.GetStride(formatId, 1);

                try
                {
                    switch (currentDesc.Format)
                    {
                        case Format.R16G16B16A16_Float:
                            for (int y1 = 0; y1 < height; y1++)
                            {
                                for (int x1 = 0; x1 < width; x1++)
                                {
                                    imageStream.Position = (long)(y1) * dataBox.RowPitch + (long)(x1) * 8;

                                    var r = Read2BytesToHalf(imageStream);
                                    var g = Read2BytesToHalf(imageStream);
                                    var b = Read2BytesToHalf(imageStream);
                                    var a = Read2BytesToHalf(imageStream);

                                    outDataStream.WriteByte((byte)(b.Clamp(0, 1) * 255));
                                    outDataStream.WriteByte((byte)(g.Clamp(0, 1) * 255));
                                    outDataStream.WriteByte((byte)(r.Clamp(0, 1) * 255));
                                    if (format == FileFormats.Png)
                                    {
                                        outDataStream.WriteByte((byte)(a.Clamp(0, 1) * 255));
                                    }
                                }
                            }
                            break;

                        case Format.R8G8B8A8_UNorm:
                            for (int y1 = 0; y1 < height; y1++)
                            {
                                imageStream.Position = (long)(y1) * dataBox.RowPitch;
                                for (int x1 = 0; x1 < width; x1++)
                                {
                                    var r = (byte)imageStream.ReadByte();
                                    var g = (byte)imageStream.ReadByte();
                                    var b = (byte)imageStream.ReadByte();
                                    outDataStream.WriteByte(b);
                                    outDataStream.WriteByte(g);
                                    outDataStream.WriteByte(r);

                                    var a = imageStream.ReadByte();
                                    if (format == FileFormats.Png)
                                    {
                                        outDataStream.WriteByte((byte)a);
                                    }
                                }
                            }
                            break;

                        case Format.R16G16B16A16_UNorm:
                            for (int y1 = 0; y1 < height; y1++)
                            {
                                imageStream.Position = (long)(y1) * dataBox.RowPitch;
                                for (int x1 = 0; x1 < width; x1++)
                                {
                                    imageStream.ReadByte(); var r = (byte)imageStream.ReadByte();
                                    imageStream.ReadByte(); var g = (byte)imageStream.ReadByte();
                                    imageStream.ReadByte(); var b = (byte)imageStream.ReadByte();
                                    outDataStream.WriteByte(b);
                                    outDataStream.WriteByte(g);
                                    outDataStream.WriteByte(r);

                                    imageStream.ReadByte(); var a = imageStream.ReadByte();
                                    if (format == FileFormats.Png)
                                    {
                                        outDataStream.WriteByte((byte)a);
                                    }
                                }
                            }
                            break;

                        default:
                            throw new InvalidOperationException($"Can't export unknown texture format {currentDesc.Format}");
                    }

                    // Copy the pixels from the buffer to the Wic Bitmap Frame encoder
                    bitmapFrameEncode.WritePixels(height, new DataRectangle(outDataStream.DataPointer, rowStride));

                    // Commit changes
                    bitmapFrameEncode.Commit();
                    encoder.Commit();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Internal image copy failed : " + e.ToString());
                }
                finally
                {
                    immediateContext.UnmapSubresource(readableImage, 0);
                    imageStream.Dispose();
                    outDataStream.Dispose();
                    bitmapFrameEncode.Dispose();
                    encoder.Dispose();
                    stream.Dispose();
                }
            } // using (imageStream)

            return true;
        }

        private static float Read4BytesToFloat(DataStream imageStream)
        {
            bytes[0] = (byte)imageStream.ReadByte();
            bytes[1] = (byte)imageStream.ReadByte();
            bytes[2] = (byte)imageStream.ReadByte();
            bytes[3] = (byte)imageStream.ReadByte();
            var r = BitConverter.ToSingle(bytes, 0);
            return r;
        }

        public static float Read2BytesToHalf(DataStream imageStream)
        {
            var low = (byte)imageStream.ReadByte();
            var high = (byte)imageStream.ReadByte();
            return ToTwoByteFloat(low, high);
        }

        public static float ToTwoByteFloat(byte ho, byte lo)
        {
            var intVal = BitConverter.ToInt32(new byte[] { ho, lo, 0, 0 }, 0);

            int mant = intVal & 0x03ff;
            int exp = intVal & 0x7c00;
            if (exp == 0x7c00) exp = 0x3fc00;
            else if (exp != 0)
            {
                exp += 0x1c000;
                if (mant == 0 && exp > 0x1c400)
                    return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | exp << 13 | 0x3ff), 0);
            }
            else if (mant != 0)
            {
                exp = 0x1c400;
                do
                {
                    mant <<= 1;
                    exp -= 0x400;
                }
                while ((mant & 0x400) == 0);

                mant &= 0x3ff;
            }

            return BitConverter.ToSingle(BitConverter.GetBytes((intVal & 0x8000) << 16 | (exp | mant) << 13), 0);
        }

        private static byte[] I2B(int input)
        {
            var bytes = BitConverter.GetBytes(input);
            return new byte[] { bytes[0], bytes[1] };
        }

        public static byte[] ToInt(float twoByteFloat)
        {
            int fbits = BitConverter.ToInt32(BitConverter.GetBytes(twoByteFloat), 0);
            int sign = fbits >> 16 & 0x8000;
            int val = (fbits & 0x7fffffff) + 0x1000;
            if (val >= 0x47800000)
            {
                if ((fbits & 0x7fffffff) >= 0x47800000)
                {
                    if (val < 0x7f800000) return I2B(sign | 0x7c00);
                    return I2B(sign | 0x7c00 | (fbits & 0x007fffff) >> 13);
                }

                return I2B(sign | 0x7bff);
            }

            if (val >= 0x38800000) return I2B(sign | val - 0x38000000 >> 13);
            if (val < 0x33000000) return I2B(sign);
            val = (fbits & 0x7fffffff) >> 23;
            return I2B(sign | ((fbits & 0x7fffff | 0x800000) + (0x800000 >> val - 102) >> 126 - val));
        }

        private static byte[] bytes = new byte[4];

        public static string LastFilename = string.Empty;
        // skip a certain number of images at the beginning since the
        // final content will only appear after several buffer flips
        public const int SkipImages = 2;
        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;

        private static readonly List<Texture2D> ImagesWithCpuAccess = new();
        private static int _currentIndex;
        private static int _currentUsageIndex;

        public static void Dispose()
        {
            foreach (var image in ImagesWithCpuAccess)
                image.Dispose();

            ImagesWithCpuAccess.Clear();
        }
    }
}