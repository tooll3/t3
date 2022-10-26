using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SpoutDX;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using DeviceContext = OpenGL.DeviceContext;
using Resource = SharpDX.DXGI.Resource;

namespace T3.Operators.Types.Id_13be1e3f_861d_4350_a94e_e083637b3e55
{
    public class SpoutOutput : Instance<SpoutOutput>
    {
        [Output(Guid = "297ea260-4486-48f8-b58f-8180acf0c2c5")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();

        public SpoutOutput()
        {
            TextureOutput.UpdateAction = Update;
            _instance++;
        }

        private void Update(EvaluationContext context)
        {
            Command.GetValue(context);
            var texture = Texture.GetValue(context);
            var senderName = SenderName.GetValue(context);

            TextureOutput.Value = texture;
            sendTexture(senderName, ref texture);

            SenderName.Update(context);
        }

        private string GetAdapterName()
        {
            IntPtr adapterName = Marshal.AllocHGlobal(1024);
            string adapter;
            unsafe
            {
                sbyte* name = (sbyte*)adapterName;
                _spoutDX.GetAdapterName(_spoutDX.Adapter, name, 1024);
                adapter = new string(name);
            }
            Marshal.FreeHGlobal(adapterName);
            return adapter;
        }

        private Texture2D CreateD3D11Texture2D(Texture2D d3d11Texture2D)
        {
            using (var resource = d3d11Texture2D.QueryInterface<Resource>())
            {
                return ResourceManager.Device.OpenSharedResource<Texture2D>(resource.SharedHandle);
            }
        }

        private bool InitializeSpout(string senderName, uint width, uint height)
        {
            if (!_initialized)
            {
                // create OpenGL context and make this become the primary context
                _deviceContext = DeviceContext.Create();
                _glContext = _deviceContext.CreateContext(IntPtr.Zero);
                _deviceContext.MakeCurrent(_glContext);
                _device = ID3D11Device.__CreateInstance(((IntPtr)ResourceManager.Device));
                _initialized = true;
            }
            else
            {
                _deviceContext.MakeCurrent(_glContext);
            }

            // get rid of old textures?
            if (width != _width || height != _height || senderName != _senderName)
            {
                DisposeTextures();

                if (_spoutDX != null)
                {
                    _spoutDX.CloseDirectX11();
                    _spoutDX.Dispose();
                    _spoutDX = null;
                }
                _width = 0;
                _height = 0;
            }

            // create new spoutDX object
            if (_spoutDX == null)
            {
                _spoutDX = new SpoutDX.SpoutDX();
                _spoutDX.OpenDirectX11(_device);
                Console.WriteLine(@$"Spout is using adapter {GetAdapterName()}");

                // create new sender and read back the actual name chosen by spout
                // (which may be different if you have multiple senders of the same name)
                _spoutDX.SenderName = senderName;
                _senderName = _spoutDX.SenderName;
                SenderName.SetTypedInputValue(_senderName);
                _width = width;
                _height = height;
            }

            return true;
        }

        private bool sendTexture(string senderName, ref Texture2D frame)
        {
            if (frame == null)
                return false;

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
                if (!InitializeSpout(senderName, (uint)width, (uint)height))
                    return false;
            }
            catch (Exception e)
            {
                Log.Debug("Initialization of Spout failed. Are Spout.dll and SpoutDX.dll present in the executable folder?");
                Log.Debug(e.ToString());
                _spoutDX?.ReleaseSender();
                _spoutDX?.CloseDirectX11();
                _spoutDX?.Dispose();
                _spoutDX = null;
                return false;
            }

            // Make this become the primary context
            if (_deviceContext == null || !_deviceContext.MakeCurrent(_glContext))
                return false;

            var device = ResourceManager.Device;
            Texture2D sharedTexture = null;
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
                        Usage = ResourceUsage.Default,
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

                // sanity check
                if (_width != width || _height != height)
                    return false;

                // don't return first two samples since buffering is not ready yet
                if (_currentUsageIndex < 0)
                {
                    _currentUsageIndex++;
                    return false;
                }

                // sharedTexture = CreateD3D11Texture2D(readableImage);
                _texture = ID3D11Texture2D.__CreateInstance(((IntPtr)readableImage));
                _spoutDX.SendTexture(_texture);
            }
            catch (Exception e)
            {
                Log.Debug("Texture sending failed : " + e.ToString());
            }
            finally
            {
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
            // dispose textures
            DisposeTextures();

            _spoutDX?.ReleaseSender();
            _spoutDX?.CloseDirectX11();
            _spoutDX?.Dispose();

            if (_instance > 0)
                --_instance;

            if (_instance <= 0)
            {
                _device?.Dispose();
                _deviceContext?.MakeCurrent(IntPtr.Zero);
                _deviceContext?.Dispose();
                _initialized = false;
            }
        }

        #endregion

        private static int _instance;                   // number of instances of this object
        private static bool _initialized;               // were static members initialized?
        private static DeviceContext _deviceContext;    // OpenGL device context
        private static IntPtr _glContext;               // OpenGL context
        private static ID3D11Device _device;            // Direct3D11 device

        private SpoutDX.SpoutDX _spoutDX;               // spout object supporting DirectX
        private string _senderName;                     // name of our spout sender
        private uint _width;                            // current width of our sender
        private uint _height;                           // current height of our sender
        private ID3D11Texture2D _texture;               // texture to send

        // skip a certain number of images at the beginning since the
        // final content will only appear after several buffer flips
        public const int SkipImages = 0;
        // hold several textures internally to speed up calculations
        private const int NumTextureEntries = 2;
        private readonly List<Texture2D> ImagesWithCpuAccess = new();
        // current image index (used for circular access of ImagesWithCpuAccess)
        private int _currentIndex;
        // current Usage index (used for implementation of image skipping)
        private int _currentUsageIndex;

        [Input(Guid = "{FE61FF9E-7F1B-4F69-9F4B-313F30B57124}")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "d4b5c642-9cb9-4f41-8739-edbb9c6c4857")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();

        [Input(Guid = "7C27EBD7-3746-4B70-A252-DD0AC0445B74")]
        public InputSlot<string> SenderName = new InputSlot<string>();
    }
}