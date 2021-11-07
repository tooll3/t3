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

namespace T3.Operators.Types.Id_ab511978_bad5_4b69_90b2_c028447fe9f7
{
    public class CurvesToTexture : Instance<CurvesToTexture>
    {
        [Output(Guid = "0322FFC8-84BD-4AA3-A59E-DEF5B212D4A1")]
        public readonly Slot<Texture2D> CurveTexture = new Slot<Texture2D>();

        

        public CurvesToTexture()
        {
            CurveTexture.UpdateAction = Update;
        }

        private float[] _floatBuffer = new float[0];
        
        private void Update(EvaluationContext context)
        {
            
            if (!Curves.IsConnected)
                return;
            
            var curveCount = Curves.CollectedInputs.Count;
            if (curveCount == 0)
                return;
            
            const int sampleCount = 256;
            const int entrySizeInBytes = sizeof(float);
            const int curveSizeInBytes = sampleCount * entrySizeInBytes;
            int bufferSizeInBytes = curveCount * curveSizeInBytes;

            using (var dataStream = new DataStream(bufferSizeInBytes, true, true))
            {
                var texDesc = new Texture2DDescription()
                                  {
                                      Width = sampleCount,
                                      Height = curveCount,
                                      ArraySize = 1,
                                      BindFlags = BindFlags.ShaderResource,
                                      Usage = ResourceUsage.Default,
                                      MipLevels = 1,
                                      CpuAccessFlags = CpuAccessFlags.None,
                                      Format = Format.R32_Float,
                                      SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                                  };

                foreach (var curveInput in Curves.CollectedInputs)
                {
                    var curve = curveInput.GetValue(context);
                    if (curve == null)
                    {
                        dataStream.Seek(curveSizeInBytes, SeekOrigin.Current);
                        continue;
                    }

                    for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                    {
                        dataStream.Write((float)curve.GetSampledValue((float)sampleIndex / sampleCount));
                    }
                }
                //Curves.DirtyFlag.Clear();

                dataStream.Position = 0;
                var dataRectangles = new DataRectangle[] { new DataRectangle(dataStream.DataPointer, curveSizeInBytes) };
                Utilities.Dispose(ref CurveTexture.Value);
                CurveTexture.Value = new Texture2D(ResourceManager.Instance().Device, texDesc, dataRectangles);
            }
        }


        [Input(Guid = "83c5a68a-2685-4506-8d79-d0db7d739102")]
        public readonly MultiInputSlot<Curve> Curves = new MultiInputSlot<T3.Core.Animation.Curve>();
    }
}