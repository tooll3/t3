using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Resource;
using T3.Core.Utils;
using Buffer = SharpDX.Direct3D11.Buffer;
using Utilities = T3.Core.Utils.Utilities;

namespace T3.Operators.Types.Id_a5f4552f_7e25_43a5_bb14_21ab836fa0b3
{
    public class PointsToCPU : Instance<PointsToCPU>
    {
        [Output(Guid = "71EF183F-E387-4382-8488-FEC2DC27B1F1")]
        public readonly Slot<StructuredList> Output = new();

        public PointsToCPU()
        {
            Output.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            var updateContinuously = UpdateContinuously.GetValue(context);
            
            try
            {
                var wasTriggered = MathUtils.WasTriggered(TriggerUpdate.GetValue(context), ref _triggerUpdate);

                if (wasTriggered)
                {
                    TriggerUpdate.SetTypedInputValue(false);
                }
                var pointBuffer = PointBuffer.GetValue(context);

                if (pointBuffer == null)
                {
                    return;
                }


                var d3DDevice = ResourceManager.Device;
                var immediateContext = d3DDevice.ImmediateContext;

                if (wasTriggered
                    || updateContinuously
                    || _bufferWithViewsCpuAccess == null
                    || _bufferWithViewsCpuAccess.Buffer == null
                    || _bufferWithViewsCpuAccess.Buffer.Description.SizeInBytes != pointBuffer.Buffer.Description.SizeInBytes
                    || _bufferWithViewsCpuAccess.Buffer.Description.StructureByteStride != pointBuffer.Buffer.Description.StructureByteStride
                   )
                {
                    try
                    {
                        if (_bufferWithViewsCpuAccess != null)
                            Utilities.Dispose(ref _bufferWithViewsCpuAccess.Buffer);

                        _bufferWithViewsCpuAccess ??= new BufferWithViews();

                        if (_bufferWithViewsCpuAccess.Buffer == null ||
                            _bufferWithViewsCpuAccess.Buffer.Description.SizeInBytes != pointBuffer.Buffer.Description.SizeInBytes)
                        {
                            _bufferWithViewsCpuAccess.Buffer?.Dispose();
                            var bufferDesc = new BufferDescription
                                                 {
                                                     Usage = ResourceUsage.Default,
                                                     BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
                                                     SizeInBytes = pointBuffer.Buffer.Description.SizeInBytes,
                                                     OptionFlags = ResourceOptionFlags.BufferStructured,
                                                     StructureByteStride = pointBuffer.Buffer.Description.StructureByteStride,
                                                     CpuAccessFlags = CpuAccessFlags.Read
                                                 };
                            _bufferWithViewsCpuAccess.Buffer = new Buffer(ResourceManager.Device, bufferDesc);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error("Failed to setup structured buffer " + e.Message, this);
                        return;
                    }

                    ResourceManager.CreateStructuredBufferSrv(_bufferWithViewsCpuAccess.Buffer, ref _bufferWithViewsCpuAccess.Srv);

                    // Keep a copy of the texture which can be accessed by CPU
                    immediateContext.CopyResource(pointBuffer.Buffer, _bufferWithViewsCpuAccess.Buffer);
                }

                // Gets a pointer to the image data, and denies the GPU access to that subresource.            
                var sourceDataBox =
                    immediateContext.MapSubresource(_bufferWithViewsCpuAccess.Buffer, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out var sourceStream);

                using (sourceStream)
                {
                    var elementCount = _bufferWithViewsCpuAccess.Buffer.Description.SizeInBytes /
                                       _bufferWithViewsCpuAccess.Buffer.Description.StructureByteStride;
                    
                    var points = sourceStream.ReadRange<Point>(elementCount);
                    
                    //Log.Debug($"Read {points.Length} elements", this);
                    Output.Value = new StructuredList<Point>(points);
                }

                immediateContext.UnmapSubresource(_bufferWithViewsCpuAccess.Buffer, 0);
                
                Output.DirtyFlag.Trigger = updateContinuously ? DirtyFlagTrigger.Animated : DirtyFlagTrigger.None;
            }
            catch (Exception e)
            {
                Log.Error("Failed to fetch GPU resource " + e.Message);
            }
        }


        private bool _triggerUpdate;
        private BufferWithViews _bufferWithViewsCpuAccess = new();

        [Input(Guid = "F267534C-59AE-4758-B04A-13B6337BC0EB")]
        public readonly InputSlot<BufferWithViews> PointBuffer = new();
        
        [Input(Guid = "EFF239DA-39E9-41D3-968B-C74723EC2545")]
        public readonly InputSlot<bool> TriggerUpdate = new();
        
        [Input(Guid = "77EE7CA9-A2DB-4DE9-BB9C-21EC4F1BBEAF")]
        public readonly InputSlot<bool> UpdateContinuously = new();

        
    }
}