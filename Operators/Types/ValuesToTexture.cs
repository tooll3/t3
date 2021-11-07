using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Animation;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Utilities = T3.Core.Utilities;

namespace T3.Operators.Types.Id_55cc0f79_96c9_482e_9794_934dc0f87708
{
    public class ValuesToTexture : Instance<ValuesToTexture>
    {
        [Output(Guid = "f01099a0-a196-4689-9900-edac07908714")]
        public readonly Slot<Texture2D> CurveTexture = new Slot<Texture2D>();

        

        public ValuesToTexture()
        {
            CurveTexture.UpdateAction = Update;
        }

        private float[] _floatBuffer = new float[0];
        
        private void Update(EvaluationContext context)
        {
            
            if (!Values.IsConnected)
                return;

            var values = Values.GetValue(context);
            if (values == null || values.Count == 0)
                return;
            

            int rangeStart = RangeStart.GetValue(context).Clamp(0, values.Count-1);
            int rangeEnd = RangeEnd.GetValue(context).Clamp(0, values.Count-1);
            
            float gain = Gain.GetValue(context);
            float pow = Pow.GetValue(context);

            if (Math.Abs(pow) < 0.001f)
            {
                return;
            }
            

            if (rangeEnd < rangeStart)
            {
                var tmp = rangeEnd;
                rangeEnd = rangeStart;
                rangeStart = tmp;
            }
            
            int sampleCount = (rangeEnd - rangeStart) + 1;
            int entrySizeInBytes = sizeof(float);
            int listSizeInBytes = sampleCount * entrySizeInBytes;
            int bufferSizeInBytes = 1 * listSizeInBytes;
            

            using (var dataStream = new DataStream(bufferSizeInBytes, true, true))
            {
                var texDesc = new Texture2DDescription()
                                  {
                                      Width = sampleCount,
                                      Height = 1,
                                      ArraySize = 1,
                                      BindFlags = BindFlags.ShaderResource,
                                      Usage = ResourceUsage.Default,
                                      MipLevels = 1,
                                      CpuAccessFlags = CpuAccessFlags.None,
                                      Format = Format.R32_Float,
                                      SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                                  };

                //foreach (var curveInput in Curves.CollectedInputs)
                //{
                    // var curve = curveInput.GetValue(context);
                    // if (curve == null)
                    // {
                    //     dataStream.Seek(curveSizeInBytes, SeekOrigin.Current);
                    //     continue;
                    // }

                    for (var sampleIndex = rangeStart; sampleIndex <= rangeEnd; sampleIndex++)
                    {
                        //dataStream.Write((float)curve.GetSampledValue((float)sampleIndex / sampleCount));
                        float v = (float)Math.Pow(values[sampleIndex] * gain, pow);
                        dataStream.Write(v );
                    }
                //}
                //Curves.DirtyFlag.Clear();

                dataStream.Position = 0;
                var dataRectangles = new DataRectangle[] { new DataRectangle(dataStream.DataPointer, listSizeInBytes) };
                Utilities.Dispose(ref CurveTexture.Value);
                CurveTexture.Value = new Texture2D(ResourceManager.Instance().Device, texDesc, dataRectangles);
            }
        }


        // [Input(Guid = "3a443955-69d4-4e96-a819-a221c71ca2cd")]
        // public readonly MultiInputSlot<Curve> Curves = new MultiInputSlot<T3.Core.Animation.Curve>();
        
        [Input(Guid = "092C8D1F-A70E-4298-B5DF-52C9D62F8E04")]
        public readonly InputSlot<List<float>> Values = new InputSlot<List<float>>();

        [Input(Guid = "CA67BFAF-EDE7-43BA-B279-FC1DDFBE2FFA")]
        public readonly InputSlot<int> RangeStart = new InputSlot<int>();

        [Input(Guid = "DB868176-D51C-41AA-BAFE-68C3E50E725E")]
        public readonly InputSlot<int> RangeEnd = new InputSlot<int>();
        
        [Input(Guid = "CC812393-F080-4E17-A525-15B09F8ACDD0")]
        public readonly InputSlot<float> Gain = new InputSlot<float>();

        [Input(Guid = "51545316-69FC-441F-B59F-44979E32972C")]
        public readonly InputSlot<float> Pow = new InputSlot<float>();
        
    }
}