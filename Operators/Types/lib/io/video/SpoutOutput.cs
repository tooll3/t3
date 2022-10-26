using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenGL;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpoutDX;
using DeviceContext = OpenGL.DeviceContext;
using Resource = SharpDX.DXGI.Resource;
// using PixelFormat = SharpDX.WIC.PixelFormat;
using T3.Core.Logging;
using SpoutDX.__Symbols;

namespace T3.Operators.Types.Id_13be1e3f_861d_4350_a94e_e083637b3e55
{
    public class SpoutOutput : Instance<SpoutOutput>
    {
        [Output(Guid = "297ea260-4486-48f8-b58f-8180acf0c2c5")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        public SpoutOutput()
        {
            ++_instance;
            _senderName = $"Tooll3 Output {_instance}";

            TextureOutput.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Command.GetValue(context);
            var texture = Texture.GetValue(context);

            TextureOutput.Value = texture;
            sendTexture(ref texture);
        }

        private unsafe bool InitializeSpout(uint width, uint height)
        {
            if (_initialized && width == _width && height == _height) return true;

            DisposeTextures();

            _deviceContext = DeviceContext.Create();
            _glContext = _deviceContext.CreateContext(IntPtr.Zero);
            // Make this become the primary context
            _deviceContext.MakeCurrent(_glContext);
        
            var deviceContext = ResourceManager.Device.ImmediateContext;
            // Create the sender
            if (!_initialized || _sender == null)
            {
                // var DXGI_FORMAT_R8G8B8A8_UINT = 30;
                var device = ID3D11Device.__CreateInstance(((IntPtr)ResourceManager.Device));
                _spoutDX = new SpoutDX.SpoutDX();
                _spoutDX.OpenDirectX11(device);
                // _spoutDX.SetAdapter(0);

                IntPtr adapterName = Marshal.AllocHGlobal(1024);
                string adapter;
                unsafe
                {
                    sbyte* name = (sbyte*)adapterName;
                    _spoutDX.GetAdapterName(_spoutDX.Adapter, name, 1024);
                    adapter = new string(name);
                }
                Marshal.FreeHGlobal(adapterName);
                Console.WriteLine(@$"Spout is using adapter {adapter}");

                _sender = new SpoutSender();
                D3D11TEXTURE2D_DESC desc = new CD3D11TEXTURE2D_DESC()
                    {
                        Width = width,
                        Height = height,
                        MipLevels = 1,
                        BindFlags = 0,
                        Usage = D3D11USAGE.D3D11USAGE_DYNAMIC,
                        CPUAccessFlags = 0,
                        //sampleCount = 1,
                        //sampleQuality = 0,
                        ArraySize = 1,
                        MiscFlags = 0
                    };
                _initialized = _sender.CreateSender(_senderName, width, height, 0);
                _spoutDX.SetActiveSender(_senderName);
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

        private Texture2D CreateD3D11Texture2D(Texture2D d3d11Texture2D)
        {
            using (var resource = d3d11Texture2D.QueryInterface<Resource>())
            {
                return ResourceManager.Device.OpenSharedResource<Texture2D>(resource.SharedHandle);
            }
        }

        private bool sendTexture(ref Texture2D frame)
        {
            if (frame == null)
                return false;

            var device = ResourceManager.Device;
            DataStream inputStream = null;
            DataStream outputStream = null;
            Texture2D sharedTexture = null;

            int width, height;
            Texture2DDescription currentDesc;
            try
            {
                currentDesc = frame.Description;
                if (currentDesc.Format != Format.R8G8B8A8_UNorm)
                {
                    Log.Debug("Spout currently only supports texture format B8G8R8A8_UNorm.");
                    Log.Debug("Please use a render target operator to change the format accordingly.");
                    return false;
                }
                width = currentDesc.Width;
                height = currentDesc.Height;
                if (!InitializeSpout((uint)width, (uint)height))
                    return false;
            }
            catch (Exception e)
            {
                Log.Debug("Initialization of Spout failed. Are Spout.dll and SpoutDX.dll present in the executable folder?");
                Log.Debug(e.ToString());
                _spoutDX?.Dispose();
                _spoutDX = null;
                return false;
            }

            // Make this become the primary context
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
                        Usage = ResourceUsage.Default, // ResourceUsage.Staging,
                        OptionFlags = ResourceOptionFlags.Shared,
                        CpuAccessFlags = CpuAccessFlags.None,
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

                sharedTexture = CreateD3D11Texture2D(readableImage);
                _texture = ID3D11Texture2D.__CreateInstance(((IntPtr)sharedTexture));
                _spoutDX.SendTexture(_texture);
            }
            catch (Exception e)
            {
                Log.Debug("Texture sending failed : " + e.ToString());
            }
            finally
            {
                inputStream?.Dispose();
                outputStream?.Dispose();
                _texture?.Dispose();
                sharedTexture?.Dispose();
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
        private SpoutDX.SpoutDX _spoutDX;
        private SpoutSender _sender;
        private ID3D11Texture2D _texture;

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