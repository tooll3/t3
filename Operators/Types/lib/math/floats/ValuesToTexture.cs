using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using T3.Core.Utils;
using Utilities = T3.Core.Utils.Utilities;

namespace T3.Operators.Types.Id_55cc0f79_96c9_482e_9794_934dc0f87708
{
    public class ValuesToTexture : Instance<ValuesToTexture>
    {
        [Output(Guid = "f01099a0-a196-4689-9900-edac07908714")]
        public readonly Slot<Texture2D> CurveTexture = new();

        public ValuesToTexture()
        {
            CurveTexture.UpdateAction = Update;
        }

        private float[] _floatBuffer = new float[0];

        private void Update(EvaluationContext context)
        {
            if (!Values.IsConnected)
                return;

            var useHorizontal = Direction.GetValue(context) == 0;
            var values = Values.GetValue(context);
            if (values == null || values.Count == 0)
                return;

            var rangeStart = RangeStart.GetValue(context).Clamp(0, values.Count - 1);
            var rangeEnd = RangeEnd.GetValue(context).Clamp(0, values.Count - 1);

            if (UseFullList.GetValue(context))
            {
                rangeStart = 0;
                rangeEnd = values.Count - 1;
            }

            var gain = Gain.GetValue(context);
            var pow = Pow.GetValue(context);

            if (Math.Abs(pow) < 0.001f)
            {
                return;
            }

            if (rangeEnd < rangeStart)
            {
                (rangeEnd, rangeStart) = (rangeStart, rangeEnd);
            }

            var sampleCount = (rangeEnd - rangeStart) + 1;
            var entrySizeInBytes = sizeof(float);
            var listSizeInBytes = sampleCount * entrySizeInBytes;
            var bufferSizeInBytes = 1 * listSizeInBytes;

            using (var dataStream = new DataStream(bufferSizeInBytes, true, true))
            {
                var texDesc = new Texture2DDescription()
                                  {
                                      Width = useHorizontal ? sampleCount : 1,
                                      Height = useHorizontal ? 1 : sampleCount,
                                      ArraySize = 1,
                                      BindFlags = BindFlags.ShaderResource,
                                      Usage = ResourceUsage.Default,
                                      MipLevels = 1,
                                      CpuAccessFlags = CpuAccessFlags.None,
                                      Format = Format.R32_Float,
                                      SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                                  };

                for (var sampleIndex = rangeStart; sampleIndex <= rangeEnd; sampleIndex++)
                {
                    float v = (float)Math.Pow(values[sampleIndex] * gain, pow);
                    dataStream.Write(v);
                }

                try
                {
                    dataStream.Position = 0;
                    var dataRectangles = new DataRectangle[]
                                             {
                                                 new(
                                                     dataPointer: dataStream.DataPointer, 
                                                     pitch: useHorizontal ? listSizeInBytes : 1 * entrySizeInBytes)
                                             };
                    Utilities.Dispose(ref CurveTexture.Value);
                    CurveTexture.Value = new Texture2D(ResourceManager.Device, texDesc, dataRectangles);
                }
                catch (Exception e)
                {
                    Log.Warning("Can't create texture from values :" + e.Message, this);
                }
            }
        }

        [Input(Guid = "092C8D1F-A70E-4298-B5DF-52C9D62F8E04")]
        public readonly InputSlot<List<float>> Values = new();

        [Input(Guid = "165F7E0E-6EF0-4BE1-8ED3-61ED0DB752ED")]
        public readonly InputSlot<bool> UseFullList = new();

        [Input(Guid = "CA67BFAF-EDE7-43BA-B279-FC1DDFBE2FFA")]
        public readonly InputSlot<int> RangeStart = new();

        [Input(Guid = "DB868176-D51C-41AA-BAFE-68C3E50E725E")]
        public readonly InputSlot<int> RangeEnd = new();

        [Input(Guid = "CC812393-F080-4E17-A525-15B09F8ACDD0")]
        public readonly InputSlot<float> Gain = new();

        [Input(Guid = "51545316-69FC-441F-B59F-44979E32972C")]
        public readonly InputSlot<float> Pow = new();

        [Input(Guid = "63E90D86-5AD5-4333-8B99-7F8D285C4913", MappedType = typeof(Directions))]
        public readonly InputSlot<int> Direction = new();

        private enum Directions
        {
            Horizontal,
            Vertical,
        }
    }
}