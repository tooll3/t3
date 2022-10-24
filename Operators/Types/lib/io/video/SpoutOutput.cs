using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System;
using System.Collections.Generic;
using OpenGL;
using Spout;
using SharpDX;
using SharpDX.DXGI;
using DeviceContext = OpenGL.DeviceContext;
using PixelFormat = SharpDX.WIC.PixelFormat;

namespace T3.Operators.Types.Id_13be1e3f_861d_4350_a94e_e083637b3e55
{
    public class SpoutOutput : Instance<SpoutOutput>
    {
        [Output(Guid = "297ea260-4486-48f8-b58f-8180acf0c2c5")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        public SpoutOutput()
        {
            TextureOutput.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Command.GetValue(context);
            var texture = Texture.GetValue(context);

            TextureOutput.Value = texture;
            sendTexture(ref texture);
        }

        private bool InitializeSpout(uint width, uint height)
        {
            if (_initialized && width == _width && height == _height) return true;

            DisposeTextures();

            if (!_initialized)
            {
                ++_instance;
                _senderName = $"Tooll3 Output {_instance}";
            }

            _deviceContext = DeviceContext.Create();
            _glContext = _deviceContext.CreateContext(IntPtr.Zero);
            // Make this become the primary context
            _deviceContext.MakeCurrent(_glContext);

            var deviceContext = ResourceManager.Device.ImmediateContext;
            // Create the sender
            if (_sender == null)
            {
                _sender = new SpoutSender();
                _initialized = _sender.CreateSender(_senderName, width, height, 0);
            }
            else
            {
                _initialized = _sender.UpdateSender(_senderName, width, height);
            }
            _width = width;
            _height = height;
            return _initialized;
        }

        // FIXME: Would possibly need some refactoring not to duplicate code from ScreenshotWriter
        private static float Read2BytesToHalf(DataStream imageStream)
        {
            var low = (byte)imageStream.ReadByte();
            var high = (byte)imageStream.ReadByte();
            return ToTwoByteFloat(low, high);
        }

        // FIXME: Would possibly need some refactoring not to duplicate code from ScreenshotWriter
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

        private bool sendTexture(ref Texture2D frame)
        {
            // Make this become the primary context
            if (frame == null)
                return false;

            var device = ResourceManager.Device;
            DataStream inputStream = null;
            DataStream outputStream = null;

            int width, height;
            Texture2DDescription currentDesc;
            try
            {
                currentDesc = frame.Description;
                width = currentDesc.Width;
                height = currentDesc.Height;
                if (!InitializeSpout((uint)width, (uint)height))
                    return false;
            }
            catch (Exception e)
            {
                return false;
            }

            if (_deviceContext == null || !_deviceContext.MakeCurrent(_glContext))
                return false;

            try
            {
                // create several textures with a given format with CPU access
                // to be able to read out the initial texture values
                if (ImagesWithCpuAccess.Count == 0
                    || ImagesWithCpuAccess[0].Description.Format != currentDesc.Format
                    || ImagesWithCpuAccess[0].Description.Width != currentDesc.Width
                    || ImagesWithCpuAccess[0].Description.Height != currentDesc.Height
                    || ImagesWithCpuAccess[0].Description.MipLevels != currentDesc.MipLevels)
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

                    DisposeTextures();

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
                immediateContext.CopyResource(frame, readableImage);
                immediateContext.UnmapSubresource(readableImage, 0);
                _currentIndex = (_currentIndex + 1) % NumTextureEntries;

                // don't return first two samples since buffering is not ready yet
                if (_currentUsageIndex++ < 0 || _width != width || _height != height)
                    return false;

                // map image resource to get a stream we can read from

                DataBox dataBox = immediateContext.MapSubresource(readableImage,
                                                                  0,
                                                                  0,
                                                                  MapMode.Read,
                                                                  SharpDX.Direct3D11.MapFlags.None,
                                                                  out inputStream);
                // Create an 8 bit RGBA output buffer to write to
                var formatId = PixelFormat.Format32bppRGBA;
                int rowStride = PixelFormat.GetStride(formatId, width);
                var pixelByteCount = PixelFormat.GetStride(formatId, 1);
                var outBufferSize = height * rowStride;
                outputStream = new DataStream(outBufferSize, true, true);

                int cbMaxLength = 0;
                int cbCurrentLength = 0;

                switch (currentDesc.Format)
                {
                    case SharpDX.DXGI.Format.R16G16B16A16_Float:
                        for (int loopY = 0; loopY < height; loopY++)
                        {
                            if (!FlipY)
                                inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                            else
                                inputStream.Position = (long)(height - 1 - loopY) * dataBox.RowPitch;

                            long outputPosition = (long)(loopY) * rowStride;

                            for (int loopX = 0; loopX < width; loopX++)
                            {
                                var b = Read2BytesToHalf(inputStream);
                                var g = Read2BytesToHalf(inputStream);
                                var r = Read2BytesToHalf(inputStream);
                                var a = Read2BytesToHalf(inputStream);

                                outputStream.WriteByte((byte)(b.Clamp(0, 1) * 255));
                                outputStream.WriteByte((byte)(g.Clamp(0, 1) * 255));
                                outputStream.WriteByte((byte)(r.Clamp(0, 1) * 255));
                                outputStream.WriteByte((byte)(a.Clamp(0, 1) * 255));
                            }
                        }
                        break;

                    case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                        for (int loopY = 0; loopY < height; loopY++)
                        {
                            if (!FlipY)
                                inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                            else
                                inputStream.Position = (long)(height - 1 - loopY) * dataBox.RowPitch;

                            for (int loopX = 0; loopX < width; loopX++)
                            {
                                byte b = (byte)inputStream.ReadByte();
                                byte g = (byte)inputStream.ReadByte();
                                byte r = (byte)inputStream.ReadByte();
                                byte a = (byte)inputStream.ReadByte();

                                outputStream.WriteByte(b);
                                outputStream.WriteByte(g);
                                outputStream.WriteByte(r);
                                outputStream.WriteByte(a);
                            }
                        }
                        break;

                    case SharpDX.DXGI.Format.R16G16B16A16_UNorm:
                        for (int loopY = 0; loopY < height; loopY++)
                        {
                            if (!FlipY)
                                inputStream.Position = (long)(loopY) * dataBox.RowPitch;
                            else
                                inputStream.Position = (long)(height - 1 - loopY) * dataBox.RowPitch;

                            for (int loopX = 0; loopX < width; loopX++)
                            {
                                inputStream.ReadByte(); byte b = (byte)inputStream.ReadByte();
                                inputStream.ReadByte(); byte g = (byte)inputStream.ReadByte();
                                inputStream.ReadByte(); byte r = (byte)inputStream.ReadByte();
                                inputStream.ReadByte(); byte a = (byte)inputStream.ReadByte();

                                outputStream.WriteByte(b);
                                outputStream.WriteByte(g);
                                outputStream.WriteByte(r);
                                outputStream.WriteByte(a);
                            }
                        }
                        break;

                    default:
                        throw new InvalidOperationException($"Can't export unknown texture format {currentDesc.Format}");
                }

                // release our resources
                immediateContext.UnmapSubresource(readableImage, 0);

                unsafe
                {
                    byte* bytePData = (byte*)outputStream.DataPointer.ToPointer();

                    _sender.SendImage(
                        bytePData, // Pixels
                        (uint)width, // Width
                        (uint)height, // Height
                        Gl.RGBA, // GL_RGBA
                        true, // B Invert
                        0 // Host FBO
                        );
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Internal image copy failed : " + e.ToString());
            }
            finally
            {
                inputStream?.Dispose();
                outputStream?.Dispose();
            }

            return true;
        }

        protected void DisposeTextures()
        {
            foreach (var image in ImagesWithCpuAccess)
                image.Dispose();

            ImagesWithCpuAccess.Clear();
        }

        #region IDisposable Support

        public new void Dispose()
        {
            if (_sender != null)
            {
                _sender.ReleaseSender();
                _sender.Dispose();
                _sender = null;
            }

            // dispose textures too
            DisposeTextures();
        }

        #endregion

        private static int _instance;
        private bool _initialized;
        private DeviceContext _deviceContext;
        private IntPtr _glContext;
        private string _senderName;
        private SpoutSender _sender;

        /// <summary>
        /// Internal use: FlipY during rendering?
        /// </summary>
        protected virtual bool FlipY
        {
            get { return true; }
        }
        private uint _width;
        private uint _height;

        // skip a certain number of images at the beginning since the
        // final content will only appear after several buffer flips
        public const int SkipImages = 2;
        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;
        private static readonly List<Texture2D> ImagesWithCpuAccess = new();
        private static int _currentIndex;
        private static int _currentUsageIndex;

        [Input(Guid = "{FE61FF9E-7F1B-4F69-9F4B-313F30B57124}")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();
        [Input(Guid = "d4b5c642-9cb9-4f41-8739-edbb9c6c4857")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();
    }
}