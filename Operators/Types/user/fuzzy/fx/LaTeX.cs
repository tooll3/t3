using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using T3.Core.Resource;
using System.IO;
using SharpDX.WIC;
using System;
using System.Windows.Media.Imaging;

using WpfMath.Parsers;
using WpfMath.Rendering;
using XamlMath;

namespace T3.Operators.Types.Id_b4127960_9824_49b2_a379_e49093ed5514
{
    public class LaTeX : Instance<LaTeX>
    {

        [Input(Guid = "7d022c0a-85a5-4ad6-b946-8582ca73f573")]
        public readonly InputSlot<string> Formula = new InputSlot<string>();

        [Input(Guid = "459d0fb8-4778-46fb-bb4c-c16c74576b5a")]
        public readonly InputSlot<float> DPI = new InputSlot<float>();

        [Input(Guid = "b4cbf757-ce16-435b-a5f8-cf98985f0e5c")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Output(Guid = "42db801a-1d93-4883-b7e1-b1bdef791900")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Out = new Slot<SharpDX.Direct3D11.Texture2D>();

        public LaTeX() { Out.UpdateAction = Update; }

        private void Update(EvaluationContext context) {
            if(Formula.DirtyFlag.IsDirty || DPI.DirtyFlag.IsDirty || Offset.DirtyFlag.IsDirty) {
                var formString = Formula.GetValue(context);
                var dpi = DPI.GetValue(context);
                // catch erroneous cases
                if(formString.Length == 0) {
                    Log.Warning("We need a latex formula");
                    return;
                }
                // wpfmath is having a hardtime to process those really tiny surfaces and give strange errors
                // in those cases. 3.0 is arbitrarily chosen to avoid these, you don't get very useful stuffs until
                // ~60.0 anyway
                if(dpi <= 3.0) {
                    Log.Warning("Can't render lesser 3.0dpi");
                    return;
                }
                if(dpi > 6000.0) {
                    Log.Warning("Capping at 6000.0dpi, beyond is very slow");
                    dpi = 6000.0F;
                }

                TexFormula formula;
                var parser = WpfTeXFormulaParser.Instance;
                var offset = Offset.GetValue(context);
                
        
                try {
                    formula = parser.Parse(formString);
                    TexEnvironment environment = WpfTeXEnvironment.Create(TexStyle.Display);
                    var source = formula.RenderToBitmap(environment, 20, offset.X, offset.Y, (int)dpi);
                    var pngBitmapEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    pngBitmapEncoder.Frames.Add(BitmapFrame.Create(source));
                    using MemoryStream memoryStream = new MemoryStream();
                    pngBitmapEncoder.Save(memoryStream);
                    ImagingFactory factory = new ImagingFactory();
                    var bmpDecoder = new SharpDX.WIC.BitmapDecoder(factory, memoryStream, DecodeOptions.CacheOnDemand);
                    var formatConverter = new FormatConverter(factory);
                    var bitmapFrameDecode = bmpDecoder.GetFrame(0);
                    formatConverter.Initialize(bitmapFrameDecode,
                        SharpDX.WIC.PixelFormat.Format32bppRGBA, BitmapDitherType.None, null, .0, BitmapPaletteType.Custom);
                    Out.Value = ResourceManager.CreateTexture2DFromBitmap(ResourceManager.Device, formatConverter);
                }
                catch(Exception e) {
                    Log.Error(e.Message);
                }
            }
        }
    }
}

