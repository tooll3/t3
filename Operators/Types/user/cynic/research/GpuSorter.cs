using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_94a85a93_7d5c_401c_930c_c3a97a32932f
{
    public class GpuSorter : Instance<GpuSorter>
    {
        [Output(Guid = "14e52376-e375-495d-a466-74731457b189")]
        public readonly Slot<Command> Command = new();

        public GpuSorter()
        {
            Command.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (_parameterConstBuffer == null)
            {
                Init();
            }

            var uav1 = BufferUav.GetValue(context);
            var uav2 = BufferUav2.GetValue(context);
            var srv1 = BufferSrv.GetValue(context);
            var srv2 = BufferSrv2.GetValue(context);
            bool spreadOverFrames = SpreadOverFrames.GetValue(context);
            if (uav1 == null || uav2 == null)
            {
                return;
            }

            var resourceManager = ResourceManager.Instance();
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var csStage = deviceContext.ComputeShader;

            var prevShader = csStage.Get();
            var prevConstBuffer = csStage.GetConstantBuffers(0, 1)[0];
            ComputeShader sortShader = _sortShaderResource.Shader;
            ComputeShader transposeShader = _transposeShaderResource.Shader;
            csStage.Set(sortShader);
            csStage.SetConstantBuffer(0, _parameterConstBuffer);
            csStage.SetUnorderedAccessView(0, uav1);

            int numBufferElements = uav1.Description.Buffer.ElementCount; //;32*1024; //512 * 512 * 2;
            int bitonicBlockSize = 1024;

            if (spreadOverFrames)
            {
                if (_level <= bitonicBlockSize)
                    // for (int level = 2; level <= bitonicBlockSize; level <<= 1)
                {
                    Int4 sortParams = new Int4(_level, _level, bitonicBlockSize, numBufferElements / bitonicBlockSize);
                    ResourceManager.SetupConstBuffer(sortParams, ref _parameterConstBuffer);
                    deviceContext.Dispatch(numBufferElements / bitonicBlockSize, 1, 1);
                }
                else
                {
                    int matWidth = bitonicBlockSize;
                    int matHeight = numBufferElements / bitonicBlockSize;
                    // Then sort the rows and columns for the levels > than the block size
                    // Transpose. Sort the Columns. Transpose. Sort the Rows.
                    // for (int level = (bitonicBlockSize * 2); level <= numBufferElements; level <<= 1)
                    // {
                    csStage.Set(transposeShader);
                    csStage.SetShaderResource(0, null);
                    csStage.SetUnorderedAccessView(0, null);
                    csStage.SetShaderResource(0, srv1);
                    csStage.SetUnorderedAccessView(0, uav2);
                    Int4 param = new Int4(_level / bitonicBlockSize, (_level & ~numBufferElements) / bitonicBlockSize, matWidth, matHeight);
                    ResourceManager.SetupConstBuffer(param, ref _parameterConstBuffer);
                    deviceContext.Dispatch(matWidth / 32, matHeight / 32, 1);

                    csStage.Set(sortShader);
                    deviceContext.Dispatch(numBufferElements / bitonicBlockSize, 1, 1);

                    csStage.Set(transposeShader);
                    csStage.SetShaderResource(0, null);
                    csStage.SetUnorderedAccessView(0, null);
                    csStage.SetShaderResource(0, srv2);
                    csStage.SetUnorderedAccessView(0, uav1);
                    param = new Int4(bitonicBlockSize, _level, matHeight, matWidth);
                    ResourceManager.SetupConstBuffer(param, ref _parameterConstBuffer);
                    deviceContext.Dispatch(matHeight / 32, matWidth / 32, 1);

                    csStage.Set(sortShader);
                    deviceContext.Dispatch(numBufferElements / bitonicBlockSize, 1, 1);
                }

                _level <<= 1;
                if (_level > numBufferElements)
                {
                    _level = 2;
                }
            }
            else
            {
                // single frame sort
                for (int level = 2; level <= bitonicBlockSize; level <<= 1)
                {
                    Int4 sortParams = new Int4(level, level, bitonicBlockSize, numBufferElements / bitonicBlockSize);
                    ResourceManager.SetupConstBuffer(sortParams, ref _parameterConstBuffer);
                    deviceContext.Dispatch(numBufferElements / bitonicBlockSize, 1, 1);
                }

                int matWidth = bitonicBlockSize;
                int matHeight = numBufferElements / bitonicBlockSize;
                // Then sort the rows and columns for the levels > than the block size
                // Transpose. Sort the Columns. Transpose. Sort the Rows.
                for (int level = (bitonicBlockSize * 2); level <= numBufferElements; level <<= 1)
                {
                    csStage.Set(transposeShader);
                    csStage.SetShaderResource(0, null);
                    csStage.SetUnorderedAccessView(0, null);
                    csStage.SetShaderResource(0, srv1);
                    csStage.SetUnorderedAccessView(0, uav2);
                    Int4 param = new Int4(level / bitonicBlockSize, (level & ~numBufferElements) / bitonicBlockSize, matWidth, matHeight);
                    ResourceManager.SetupConstBuffer(param, ref _parameterConstBuffer);
                    deviceContext.Dispatch(matWidth / 32, matHeight / 32, 1);

                    csStage.Set(sortShader);
                    deviceContext.Dispatch(numBufferElements / bitonicBlockSize, 1, 1);

                    csStage.Set(transposeShader);
                    csStage.SetShaderResource(0, null);
                    csStage.SetUnorderedAccessView(0, null);
                    csStage.SetShaderResource(0, srv2);
                    csStage.SetUnorderedAccessView(0, uav1);
                    param = new Int4(bitonicBlockSize, level, matHeight, matWidth);
                    ResourceManager.SetupConstBuffer(param, ref _parameterConstBuffer);
                    deviceContext.Dispatch(matHeight / 32, matWidth / 32, 1);

                    csStage.Set(sortShader);
                    deviceContext.Dispatch(numBufferElements / bitonicBlockSize, 1, 1);
                }
            }

            csStage.SetUnorderedAccessView(0, null);
            csStage.SetUnorderedAccessView(1, null);
            csStage.SetShaderResource(0, null);
            csStage.SetShaderResource(1, null);
            csStage.SetConstantBuffer(0, prevConstBuffer);
            csStage.Set(prevShader);
        }

        public void Init()
        {
            var resourceManager = ResourceManager.Instance();

            if (_sortShaderResource == null)
            {
                string sourcePath = @"Resources\proj-partial\particle\bitonic-sort.hlsl";
                string entryPoint = "bitonicSort";
                string debugName = "bitonic-sort";
                resourceManager.TryCreateShaderResource(resource: out _sortShaderResource, 
                                                        fileName: sourcePath, 
                                                        errorMessage: out var errorMessage, 
                                                        name: debugName, 
                                                        entryPoint: entryPoint);
            }

            if (_transposeShaderResource == null)
            {
                string sourcePath = @"Resources\proj-partial\particle\bitonic-transpose.hlsl";
                string entryPoint = "transpose";
                string debugName = "bitonic-transpose";
                resourceManager.TryCreateShaderResource(resource: out _transposeShaderResource, 
                                                        fileName: sourcePath, 
                                                        errorMessage: out var errorMessage, 
                                                        name: debugName, 
                                                        entryPoint: entryPoint);
            }

            InitConstBuffer();
        }

        private void InitConstBuffer()
        {
            ResourceManager.SetupConstBuffer(Int4.Zero, ref _parameterConstBuffer);
            _parameterConstBuffer.DebugName = "GpuSort-ParameterConstBuffer";
        }

        private Buffer _parameterConstBuffer;
        private ShaderResource<ComputeShader> _sortShaderResource;
        private ShaderResource<ComputeShader> _transposeShaderResource;

        [Input(Guid = "37dddd93-2b54-4598-aaca-40710ed06417")]
        public readonly InputSlot<SharpDX.Direct3D11.UnorderedAccessView> BufferUav = new();

        [Input(Guid = "79d7bbd1-37a3-49eb-b705-e39345b50568")]
        public readonly InputSlot<SharpDX.Direct3D11.UnorderedAccessView> BufferUav2 = new();

        [Input(Guid = "187b350b-71da-4cad-9e44-6f536e647e97")]
        public readonly InputSlot<SharpDX.Direct3D11.ShaderResourceView> BufferSrv = new();

        [Input(Guid = "5dfdc602-00c9-4125-b49d-ca15c769f43e")]
        public readonly InputSlot<SharpDX.Direct3D11.ShaderResourceView> BufferSrv2 = new();

        [Input(Guid = "8c4e6ec3-5de6-4477-9ea1-6a5fd173e784")]
        public readonly InputSlot<bool> SpreadOverFrames = new();

        private int _level = 2;
    }
}