using System.Diagnostics;
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Animation;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Utilities = T3.Core.Utilities;

namespace T3.Operators.Types.Id_2c53eee7_eb38_449b_ad2a_d7a674952e5b
{
    public class GradientsToTexture : Instance<GradientsToTexture>
    {
        [Output(Guid = "7ad741ec-274d-493c-994f-1a125b96a6e9")]
        public readonly Slot<Texture2D> GradientsTexture = new Slot<Texture2D>();

        

        public GradientsToTexture()
        {
            GradientsTexture.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            if (!Gradients.IsConnected)
                return;
            
            var gradientsCount = Gradients.CollectedInputs.Count;
            if (gradientsCount == 0)
                return;
            
            const int sampleCount = 256;
            const int entrySizeInBytes = sizeof(float) * 4;
            const int gradientSizeInBytes = sampleCount * entrySizeInBytes;
            int bufferSizeInBytes = gradientsCount * gradientSizeInBytes;

            using (var dataStream = new DataStream(bufferSizeInBytes, true, true))
            {
                var texDesc = new Texture2DDescription()
                                  {
                                      Width = sampleCount,
                                      Height = gradientsCount,
                                      ArraySize = 1,
                                      BindFlags = BindFlags.ShaderResource,
                                      Usage = ResourceUsage.Default,
                                      MipLevels = 1,
                                      CpuAccessFlags = CpuAccessFlags.None,
                                      Format = Format.R32G32B32A32_Float,
                                      SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                                  };

                foreach (var gradientsInput in Gradients.CollectedInputs)
                {
                    var gradient = gradientsInput.GetValue(context);
                    if (gradient == null)
                    {
                        dataStream.Seek(gradientSizeInBytes, SeekOrigin.Current);
                        continue;
                    }

                    for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                    {
                        var sampledColor = gradient.Sample((float)sampleIndex / sampleCount);
                        dataStream.Write(sampledColor.X);
                        dataStream.Write(sampledColor.Y);
                        dataStream.Write(sampledColor.Z);
                        dataStream.Write(sampledColor.W);
                    }
                }
                //Curves.DirtyFlag.Clear();

                dataStream.Position = 0;
                var dataRectangles = new DataRectangle[] { new DataRectangle(dataStream.DataPointer, gradientSizeInBytes) };
                Utilities.Dispose(ref GradientsTexture.Value);
                GradientsTexture.Value = new Texture2D(ResourceManager.Instance().Device, texDesc, dataRectangles);
            }
        }

        
        [Input(Guid = "588BE11F-D0DB-4E51-8DBB-92A25408511C")]
        public readonly MultiInputSlot<Gradient> Gradients = new MultiInputSlot<Gradient>();
        
    }
}