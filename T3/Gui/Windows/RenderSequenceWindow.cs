using System;
using System.Collections.Generic;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.IO;
using SharpDX.WIC;
using T3.Core;
using T3.Gui.Windows.Output;
using Vector2 = System.Numerics.Vector2;

namespace T3.Gui.Windows
{
    public class RenderSequenceWindow : Window, IDisposable
    {
        public RenderSequenceWindow()
        {
            Config.Title = "Render Sequence";
        }

        private bool _pickResult;
        
        protected override void DrawContent()
        {
            ImGui.Text("Render Sequence");
            var mainTexture = OutputWindow.GetPrimaryOutputWindow()?.GetCurrentTexture();
            if (ImGui.Button("Save Image"))
            {
                _pickResult = SaveBufferToFile(new Vector2(0.5f, 0.5f), mainTexture);
            }

            ImGui.Text($"Saved: {_lastFilename}");
        }
        
        bool SaveBufferToFile(Vector2 screenPosition, Texture2D texture2d)
        {
            var device = ResourceManager.Instance().Device;            
            
            if (screenPosition.X < -1 || screenPosition.X > 1 ||
                screenPosition.Y < -1 || screenPosition.Y > 1)
                return false;

            if (texture2d == null)
                return false;

            var currentDesc = texture2d.Description;
            if (_imagesWithCpuAccess.Count == 0 
                || _imagesWithCpuAccess[0].Description.Format != currentDesc.Format 
                || _imagesWithCpuAccess[0].Description.Width != currentDesc.Width 
                || _imagesWithCpuAccess[0].Description.Height != currentDesc.Height 
                || _imagesWithCpuAccess[0].Description.MipLevels != currentDesc.MipLevels)
            {    
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

                Dispose();
                
                for (int i = 0; i < NumTextureEntries; ++i)
                {
                    _imagesWithCpuAccess.Add(new Texture2D(device, imageDesc));
                }
                _currentIndex = 0;
                _currentUsageIndex = 0;
            }

            var immediateContext = device.ImmediateContext;
            var readableImage = _imagesWithCpuAccess[_currentIndex];
            immediateContext.CopyResource(texture2d, readableImage);
            _currentIndex = ++_currentIndex % NumTextureEntries;
            ++_currentUsageIndex;

            if (_currentUsageIndex >= NumTextureEntries)
            {
                var x = (int)(currentDesc.Width*(screenPosition.X + 1.0f)*0.5f).Clamp(0, currentDesc.Width - 1);
                var y = (int)(currentDesc.Height*(screenPosition.Y - 1.0f)*-0.5f).Clamp(0, currentDesc.Height - 1);

                immediateContext.UnmapSubresource(readableImage, 0);
                
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
                    const string filename = "output.jpg";

                    var factory = new ImagingFactory();

                    WICStream stream = null;

                    stream = new WICStream(factory, filename, NativeFileAccess.Write);

                    // Initialize a Jpeg encoder with this stream
                    //var encoder = new PngBitmapEncoder(factory);
                    var encoder = new JpegBitmapEncoder(factory);
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
                    
                    for (int y1 = 0; y1 < height; y1++)
                    {
                        for (int x1 = 0; x1 < width; x1++)
                        {
                            imageStream.Position = (long)(y1 )* dataBox.RowPitch + (long)(x1) * 8;
                            
                            var r = Read2BytesToHalf(imageStream);
                            var g = Read2BytesToHalf(imageStream);
                            var b = Read2BytesToHalf(imageStream);
                            var a = Read2BytesToHalf(imageStream);

                            outDataStream.WriteByte( (byte)(b.Clamp(0,1)*255));
                            outDataStream.WriteByte( (byte)(g.Clamp(0,1)*255));
                            outDataStream.WriteByte( (byte)(r.Clamp(0,1)*255));
                        }
                    }
                    
                    // Copy the pixels from the buffer to the Wic Bitmap Frame encoder
                    bitmapFrameEncode.WritePixels(height, new DataRectangle(outDataStream.DataPointer, rowStride));

                    // Commit changes
                    bitmapFrameEncode.Commit();
                    encoder.Commit();
                    bitmapFrameEncode.Dispose();
                    encoder.Dispose();
                    stream.Dispose();
                    
                    immediateContext.UnmapSubresource(readableImage, 0);
                }
            }
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
        
        private static float Read2BytesToHalf(DataStream imageStream)
        {
            var low = (byte)imageStream.ReadByte();
            var high = (byte)imageStream.ReadByte();
            return toTwoByteFloat(low, high);;
        }

        public static float toTwoByteFloat(byte HO, byte LO)
        {
            var intVal = BitConverter.ToInt32(new byte[] { HO, LO, 0, 0 }, 0);

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
                } while ((mant & 0x400) == 0);
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

        private string _lastFilename=string.Empty;
        private const int NumTextureEntries = 2;
        
        //private bool _lastEvaluationSucceeded;
        //private Texture2D _depthImageWithCPUAccess;
        
        //private float _lastEvaluatedDepth;
        private readonly List<Texture2D> _imagesWithCpuAccess = new List<Texture2D>();
        private int _currentIndex;
        private int _currentUsageIndex;        
        
        public void Dispose()
        {
            foreach (var image in _imagesWithCpuAccess)
                image.Dispose();
            _imagesWithCpuAccess.Clear();
        }

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }
    }
}