using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Collections.Generic;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using T3.Core.Operator.Interfaces;
using SharpDX;
using T3.Core.Resource;
using DirectShowLib;
using Rectangle = System.Drawing.Rectangle;

namespace T3.Operators.Types.Id_cd5a182e_254b_4e65_820b_ff754112614c
{
    public class VideoDeviceInput : Instance<VideoDeviceInput>, ICustomDropdownHolder
    {
        [Output(Guid = "1d0159cc-33d2-46b1-9c0c-7054aa560df5", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> Texture = new();

        public VideoDeviceInput()
        {
            Texture.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            ScanWebCamDevices(); // Result will be reused

            var deviceName = InputDeviceName.GetValue(context);

            if (!TryGetIndexForDeviceName(deviceName, out var selectedDeviceIndex))
            {
                Log.Debug($"Can't find web camera {deviceName}");
                return;
            }

            if (selectedDeviceIndex != _storeDeviceIndex)
            {
                if (_capture != null)
                {
                    _capture.ImageGrabbed -= ImageGrabbedHandler;
                    _capture.Stop();
                    _capture.Dispose();
                    _capture = null;
                }

                Log.Debug($"Switching to {selectedDeviceIndex}  {deviceName}", this);
                //_capture = new VideoCapture(selectedDeviceIndex);
                _capture = new VideoCapture(selectedDeviceIndex, VideoCapture.API.DShow);
                _storeDeviceIndex = selectedDeviceIndex;

                _capture.Set(CapProp.Fps, 60);
                _capture.ImageGrabbed += ImageGrabbedHandler;
                _capture.Start();
            }

            if (_capture != null && _grabbedNewFrame && _bitmap != null)
            {
                UploadBitmap(ResourceManager.Device, _bitmap);
                Texture.Value = _gpuTexture;
                _grabbedNewFrame = false;
            }
        }

        private bool _grabbedNewFrame;
        private byte[] _frameData = Array.Empty<byte>();

        private void ImageGrabbedHandler(object sender, EventArgs e)
        {
            if (sender is not VideoCapture capture)
                return;
            var frame = capture.QueryFrame();
            if (frame == null) return;

            // 1) Convert to BGRA in one step
            var mat = new Mat();
            CvInvoke.CvtColor(frame, mat, ColorConversion.Bgr2Bgra);

            // 2) Get raw BGRA bytes from that Mat
            var width = mat.Cols;
            var stride = width * 4; // for 8-bit BGRA
            var height = mat.Rows;
            var dataSize = height * stride;
            if (_frameData.Length  != dataSize)
            {
                _frameData = new byte[dataSize];
            }
            
            Marshal.Copy(mat.DataPointer, _frameData, 0, dataSize);

            // 3) Upload those bytes to a GPU texture if desired
            // E.g. deviceContext.UpdateSubresource(...)

            // 4) If you still need a Bitmap for preview, convert once:
            using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(_frameData, 0, bmpData.Scan0, _frameData.Length);
            bmp.UnlockBits(bmpData);

            _bitmap = (Bitmap)bmp.Clone(); // store for UI or whatever
            _grabbedNewFrame = true;
        }

        public void UploadBitmap(SharpDX.Direct3D11.Device device, Bitmap bitmap)
        {
            // 1. Convert to 32bpp if needed
            Bitmap src = bitmap;
            if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                src = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(src);
                g.DrawImage(bitmap, 0, 0);
            }

            // 2. Lock bits
            var rect = new Rectangle(0, 0, src.Width, src.Height);
            var bmpData = src.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            // 3. If our texture doesn't exist or size changed, recreate it
            if (_gpuTexture == null || _width != src.Width || _height != src.Height)
            {
                _gpuTexture?.Dispose();
                _width = src.Width;
                _height = src.Height;

                var texDesc = new Texture2DDescription
                                  {
                                      Width = _width,
                                      Height = _height,
                                      MipLevels = 1,
                                      ArraySize = 1,
                                      Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                                      SampleDescription = new SampleDescription(1, 0),
                                      Usage = ResourceUsage.Default,
                                      BindFlags = BindFlags.ShaderResource,
                                      CpuAccessFlags = CpuAccessFlags.None,
                                      OptionFlags = ResourceOptionFlags.None
                                  };

                // Create initial texture with the locked bitmap data
                //_gpuTexture = new Texture2D(device, texDesc, new DataRectangle(bmpData.Scan0, bmpData.Stride));
                var dataRectangles = new DataRectangle(bmpData.Scan0, bmpData.Stride);
                _gpuTexture = new Texture2D(device, texDesc, dataRectangles);
            }
            else
            {
                // 4. If texture exists and size matches, update subresource
                device.ImmediateContext.UpdateSubresource(
                                                          new DataBox(bmpData.Scan0, bmpData.Stride, 0), _gpuTexture, 0);
            }

            // 5. Unlock
            src.UnlockBits(bmpData);
            if (!ReferenceEquals(src, bitmap))
                src.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_capture != null)
            {
                _capture.Stop();
                _capture.Dispose();
                _capture = null;
            }
        }

        #region device dropdown
        string ICustomDropdownHolder.GetValueForInput(Guid inputId)
        {
            return InputDeviceName.Value;
        }

        IEnumerable<string> ICustomDropdownHolder.GetOptionsForInput(Guid inputId)
        {
            if (WebcamWithIndices == null || WebcamWithIndices.Count == 0)
            {
                yield return "undefined";
                yield break;
            }

            foreach (var (webcam, _) in WebcamWithIndices)
            {
                yield return webcam;
            }
        }

        void ICustomDropdownHolder.HandleResultForInput(Guid inputId, string result)
        {
            InputDeviceName.SetTypedInputValue(result);
        }
        #endregion

        #region scanning for webcams (shared across all instances...
        public static void ScanWebCamDevices()
        {
            var alreadyInitializedByOtherOp = WebcamWithIndices != null;
            if (alreadyInitializedByOtherOp)
                return;

            var moniker = new IMoniker[100];

            WebcamWithIndices = new List<WebcamWithIndex>();

            // Create system device enumerator
            var srvType = Type.GetTypeFromCLSID(SystemDeviceEnum);
            if (srvType == null)
            {
                Log.Warning("Failed initialize webcams: Can't get type for SystemDeviceEnum");
                return;
            }

            var comObj = Activator.CreateInstance(srvType);

            var enumDev = (ICreateDevEnum)comObj;
            if (enumDev == null)
            {
                Log.Warning("Failed initialize webcams: Can't initialize enumerator");
                return;
            }

            // Create an enumerator to find filters of specified category
            enumDev.CreateClassEnumerator(VideoInputDevice, out var enumMon, 0);
            var bagId = typeof(IPropertyBag).GUID;

            var camIndex = -1; // start with -1 so we can increment with continue
            while (enumMon.Next(1, moniker, IntPtr.Zero) == 0)
            {
                camIndex++;
                try
                {
                    // get property bag of the moniker
                    moniker[0].BindToStorage(null, null, ref bagId, out var bagObj);
                    if (bagObj is not IPropertyBag bag)
                    {
                        Log.Warning("Failed to read webcam name " + camIndex);
                        continue;
                    }

                    bag.Read("FriendlyName", out var nameObj, null);
                    if (nameObj is not string name || string.IsNullOrEmpty(name))
                    {
                        Log.Warning("Failed to read webcam name " + camIndex);
                        continue;
                    }

                    WebcamWithIndices.Add(new WebcamWithIndex(name, camIndex));
                }
                catch (Exception e)
                {
                    Log.Debug("Failed to Scan webcam: " + e.Message);
                }
            }

            if (WebcamWithIndices.Count == 0)
            {
                Log.Debug("  No cameras found");
            }
            else
            {
                foreach (var (name, index) in WebcamWithIndices)
                {
                    Log.Debug($"  Cameras found: #{index}: {name}");
                }
            }
        }

        private static bool TryGetIndexForDeviceName(string deviceName, out int index)
        {
            index = -1;
            if (WebcamWithIndices == null || WebcamWithIndices.Count == 0)
                return false;

            foreach (var (name, i) in WebcamWithIndices)
            {
                if (name != deviceName)
                    continue;

                index = i;
                return true;
            }

            return false;
        }

        public record WebcamWithIndex(string Name, int Index);

        public static List<WebcamWithIndex> WebcamWithIndices;
        internal static readonly Guid SystemDeviceEnum = new Guid(0x62BE5D10, 0x60EB, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);
        internal static readonly Guid VideoInputDevice = new Guid(0x860BB310, 0x5D01, 0x11D0, 0xBD, 0x3B, 0x00, 0xA0, 0xC9, 0x11, 0xCE, 0x86);
        #endregion

        private VideoCapture _capture;
        private int _storeDeviceIndex = -1;
        private System.Drawing.Bitmap _bitmap;

        private Texture2D _gpuTexture;
        private int _width;
        private int _height;

        [Input(Guid = "f5b900ec-ee17-123e-9972-cdd0580c104e" /*, MappedType = typeof(InputDevices)*/)]
        public readonly InputSlot<string> InputDeviceName = new();
    }
}