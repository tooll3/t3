using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using T3.Core.Resource;
using ImageMagick;
using System.IO;
using SharpDX.WIC;
using SharpDX.Direct3D11;
using System;


namespace T3.Operators.Types.Id_cf473b55_5fa1_4dfd_854d_caa763d33cdd
{
    public class PDF : Instance<PDF>
    {

        [Output(Guid = "0c2bb090-8698-43f5-bd69-a74a42c12519")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Texture = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "5ca4eab7-4254-4113-9836-037d68444bb2")]
        public readonly InputSlot<string> Filename = new InputSlot<string>();

        [Input(Guid = "1a3a8db9-3db7-4c59-bd9f-50e6b7d5ab04")]
        public readonly InputSlot<int> DPI = new InputSlot<int>();

        public PDF() {
            Texture.UpdateAction = Update;
        }

        private void Update(EvaluationContext context) {
            if(Filename.DirtyFlag.IsDirty) {
                ResourceFileWatcher.AddFileHook(Filename.GetValue(context), () => {Filename.DirtyFlag.Invalidate();});
            }
            UpdatePDF(context);
        }

        private void UpdatePDF(EvaluationContext context) {
            var dpi = DPI.GetValue(context);
            if (dpi < 1) {
                Log.Warning("Can't render a texture with lesser than 1dpi");
                return;
            }
            var settings = new MagickReadSettings {
                Density = new Density(dpi, dpi)
            };
            using var memStream = new MemoryStream();
            try {
                if(!File.Exists(Filename.GetValue(context))) {
                    Log.Warning("file doesn't exist");
                    return;
                }
                using var imgs = new MagickImageCollection();
                imgs.Read(Filename.GetValue(context), settings);
                var img = imgs[0];
                img.Format = MagickFormat.Png;
                img.Write(memStream);
                ImagingFactory factory = new ImagingFactory();
                memStream.Position = 0;
                var bmpDecoder = new BitmapDecoder(factory, memStream, DecodeOptions.CacheOnDemand);
                var formatConverter = new FormatConverter(factory);
                var bitmapFrameDecode = bmpDecoder.GetFrame(0);
                formatConverter.Initialize(bitmapFrameDecode, SharpDX.WIC.PixelFormat.Format32bppRGBA,
                    BitmapDitherType.None, null, .0, BitmapPaletteType.Custom);
                Texture.Value = ResourceManager.CreateTexture2DFromBitmap(ResourceManager.Device, formatConverter);
            }
            catch (Exception e) {
                Log.Info(e.Message);
            }
        }
    }
}

