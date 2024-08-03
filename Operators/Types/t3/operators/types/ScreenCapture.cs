using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using NewTek.NDI;
using NewTek;
using System.Linq;
using SharpGLTF.Schema2;
using SharpDX.Direct2D1;
using System.Threading;

namespace Operators.Types.t3.operators.types
{
    [Guid("80032e95-90ec-486d-92a4-ff3d225e556e")]
    public class ScreenCapture : Instance<ScreenCapture>
    {
        [Output(Guid = "16D64181-ABFE-4B34-98C3-87B88D049E50", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Input(Guid = "633199D4-1950-48E3-ACE9-0E266C7E5771")]
        public InputSlot<int> ScreenIndex = new();

        [Input(Guid = "9ACD5B21-CAC7-4A1E-B7A8-1552AD0A907D")]
        public InputSlot<int> TimeOut = new();

        private OutputDuplication dup;
        private int currentScreenIndex = -1;

        private SharpDX.Direct3D11.Texture2D currentScreen;

        public ScreenCapture()
        {
            TextureOutput.UpdateAction = Update;
        }
        ~ScreenCapture()
        {
            Dispose();
        }


        private void Update(EvaluationContext context)
        {
            var device = ResourceManager.Device;
            var screenIndex = ScreenIndex.GetValue(context);
            var timeOut = TimeOut.GetValue(context);

            if (currentScreenIndex != screenIndex)
            {
                Utilities.Dispose(ref dup);
                currentScreenIndex = screenIndex;
            }

            if (dup == null)
            {
                using (var dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device>())
                {
                    var dxgiAdapter = dxgiDevice.GetParent<SharpDX.DXGI.Adapter>();

                    Output output = dxgiAdapter.GetOutput(Utilities.InfiniteModIndexer(screenIndex, dxgiAdapter.GetOutputCount()));

                    using (Output1 o1 = output.QueryInterface<Output1>())
                    {
                        dup = o1.DuplicateOutput((SharpDX.Direct3D11.Device)device);
                    }
                }
            }

            OutputDuplicateFrameInformation info;
            SharpDX.DXGI.Resource newScreenResource;

            var capResult = dup.TryAcquireNextFrame(timeOut, out info, out newScreenResource);

            if (capResult.Success)
            {
                using (SharpDX.Direct3D11.Texture2D newTexture = newScreenResource.QueryInterface<SharpDX.Direct3D11.Texture2D>())
                {
                    if (currentScreen != null)
                    {
                        if (currentScreen.Description.Width != newTexture.Description.Width
                            || currentScreen.Description.Height != newTexture.Description.Height
                            || currentScreen.Description.Format != newTexture.Description.Format)
                        {
                            Utilities.Dispose(ref currentScreen);
                        }
                    }

                    if (currentScreen == null)
                    {
                        var desc = newTexture.Description;
                        desc.OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None;
                        desc.BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource;
                        currentScreen = new SharpDX.Direct3D11.Texture2D(device, desc);
                        //using (var newTex = new SharpDX.Direct3D11.Texture2D((SharpDX.Direct3D11.Device)device, desc))
                        //{
                        //    currentScreen = new Texture2dReadView(newTex, device.ResourceFactory.ResourceTracker);
                        //}
                    }
                    device.ImmediateContext.CopyResource(newTexture, currentScreen);
                }

                dup.ReleaseFrame();
            }

            Utilities.Dispose(ref newScreenResource);

            TextureOutput.Value = currentScreen;

        }

        #region IDisposable Support
        public new void Dispose()
        {
            Utilities.Dispose(ref currentScreen);
            Utilities.Dispose(ref dup);
        }

        #endregion

    }
}