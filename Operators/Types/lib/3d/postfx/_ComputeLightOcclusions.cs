using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;
using Utilities = T3.Core.Utils.Utilities;

namespace T3.Operators.Types.Id_de0e54c3_631b_4a01_a8a7_8cdff2e07e55
{
    public class _ComputeLightOcclusions : Instance<_ComputeLightOcclusions>
    {
        [Output(Guid = "D6A7B2CF-740E-4B52-8BB2-BC786F2C39AB")]
        public readonly Slot<float> Output = new();

        public _ComputeLightOcclusions()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (UpdateCommand.IsConnected && UpdateCommand.DirtyFlag.IsDirty)
            {
                // This will execute the input
                UpdateCommand.GetValue(context);
            }

            var lightIndex = LightIndex.GetValue(context).Clamp(0, 7);            
            var inputImage = InputImage.GetValue(context);
            
            if (inputImage == null)
            {
                return;
            }

            var d3DDevice = ResourceManager.Device;
            var immediateContext = d3DDevice.ImmediateContext;

            // keep a copy of the texture which can be accessed by CPU
            // TODO: This should be a cycle buffer with 3 images that are only created once 
            var desc = new Texture2DDescription()
                           {
                               BindFlags = BindFlags.None,
                               Format = inputImage.Description.Format,
                               Width = inputImage.Description.Width,
                               Height = inputImage.Description.Height,
                               MipLevels = inputImage.Description.MipLevels,
                               SampleDescription = new SampleDescription(1, 0),
                               Usage = ResourceUsage.Staging,
                               OptionFlags = ResourceOptionFlags.None,
                               CpuAccessFlags = CpuAccessFlags.Read, 
                               ArraySize = 1
                           };
            Utilities.Dispose(ref _imageWithCpuAccess);
            _imageWithCpuAccess = new Texture2D(d3DDevice, desc);
            immediateContext.CopyResource(inputImage, _imageWithCpuAccess);

            // Gets a pointer to the image data, and denies the GPU access to that subresource.            
            immediateContext.MapSubresource(_imageWithCpuAccess, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out var sourceStream);

            using var stream = sourceStream;
            float result = 0;
                

            switch (inputImage.Description.Format)
            {
                case Format.R32_Float:
                {
                    try
                    {
                        sourceStream.Seek( sizeof(float) * lightIndex, System.IO.SeekOrigin.Begin);
                        result = sourceStream.Read<float>();
                    }
                    catch (Exception e)
                    {
                        Log.Warning("Failed to convert light data: " + e.Message);
                    }
                    break;
                }
                    
                default:
                    Log.Warning($"Can't access unknown texture format {inputImage.Description.Format}", this);
                    break;
            }

            Output.Value = result;
            immediateContext.UnmapSubresource(_imageWithCpuAccess, 0);
        }

        private Texture2D _imageWithCpuAccess;
        
        [Input(Guid = "088ddcee-1407-4cd8-85bc-6704b8ea73b1")]
        public readonly InputSlot<Command> UpdateCommand = new();

        [Input(Guid = "d2147f2d-04dd-47aa-8cab-5b588e178a1f")]
        public readonly InputSlot<Texture2D> InputImage = new();

        [Input(Guid = "2869E416-7D0B-4EF5-B25B-5794FD840848")]
        public readonly InputSlot<int> LightIndex = new();


    }
}