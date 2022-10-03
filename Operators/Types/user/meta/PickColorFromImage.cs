using System;
using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace T3.Operators.Types.Id_42703423_1414_489e_aac2_21a3d7204262
{
    public class PickColorFromImage : Instance<PickColorFromImage>
    {
        [Output(Guid = "4f0c5c55-74b3-46d9-bbbc-4aad5dc14ea3")]
        public readonly Slot<Vector4> Output = new();

        Texture2D _imageWithCPUAccess;

        public PickColorFromImage()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var inputImage = InputImage.GetValue(context);
            var row = Row.GetValue(context);
            var column = Column.GetValue(context);

            if (inputImage == null)
            {
                return;
            }
            
            var d3DDevice = ResourceManager.Instance().Device;
            var immediateContext = d3DDevice.ImmediateContext;

            if (_imageWithCPUAccess == null ||
                _imageWithCPUAccess.Description.Format != inputImage.Description.Format ||
                _imageWithCPUAccess.Description.Width != inputImage.Description.Width ||
                _imageWithCPUAccess.Description.Height != inputImage.Description.Height ||
                _imageWithCPUAccess.Description.MipLevels != inputImage.Description.MipLevels)
            {
                // keep a copy of the texture which can be accessed by CPU
                var desc = new Texture2DDescription()
                {
                    BindFlags = BindFlags.None,
                    Format = Format.R8G8B8A8_UNorm,
                    Width = inputImage.Description.Width,
                    Height = inputImage.Description.Height,
                    MipLevels = inputImage.Description.MipLevels,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging,
                    OptionFlags = ResourceOptionFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read, // <- that we want
                    ArraySize = 1
                };
                Utilities.Dispose(ref _imageWithCPUAccess);
                _imageWithCPUAccess = new Texture2D(d3DDevice, desc);
                immediateContext.CopyResource(inputImage, _imageWithCPUAccess);
            }

            var width = inputImage.Description.Width;
            var height = inputImage.Description.Height;

            column %= width;
            row %= height;

            // Gets a pointer to the image data, and denies the GPU access to that subresource.            
            SharpDX.DataStream sourceStream;
            var sourceDataBox =
                immediateContext.MapSubresource(_imageWithCPUAccess, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out sourceStream);

            using (sourceStream)
            {
                // position to the wanted pixel. FIXME: 4 should be replaced by correct # of bytes per pixel
                sourceStream.Seek(row * sourceDataBox.RowPitch + 4 * column, System.IO.SeekOrigin.Begin);
                var color = new SharpDX.Color4(sourceStream.Read<Int32>()); // FIXME Int32 implies 4 byte per pixel 
                Output.Value = new Vector4(color.Red, color.Green, color.Blue, color.Alpha);
            }

            immediateContext.UnmapSubresource(_imageWithCPUAccess, 0);
        }

        [Input(Guid = "3b8c51c9-c544-47eb-9d70-4bd6b161be2d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> InputImage = new();

        [Input(Guid = "3b1a9402-439f-46ff-9993-4fe806757b65")]
        public readonly InputSlot<int> Row = new();

        [Input(Guid = "8cd44c6d-8096-4ca3-b4df-4f723438ae7b")]
        public readonly InputSlot<int> Column = new();
        
    }
}