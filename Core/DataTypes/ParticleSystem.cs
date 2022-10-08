using System.Linq;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using T3.Core.Logging;

namespace T3.Core.DataTypes
{
    public class ParticleSystem
    {
        public Buffer ParticleBuffer;
        public UnorderedAccessView ParticleBufferUav;
        public ShaderResourceView ParticleBufferSrv;

        public Buffer DeadParticleIndices;
        public UnorderedAccessView DeadParticleIndicesUav;

        public Buffer AliveParticleIndices;
        public UnorderedAccessView AliveParticleIndicesUav;
        public ShaderResourceView AliveParticleIndicesSrv;

        public Buffer IndirectArgsBuffer;
        public UnorderedAccessView IndirectArgsBufferUav;

        public Buffer ParticleCountConstBuffer;

        public int MaxCount { get; set; } = 20480;
        public readonly int ParticleSizeInBytes = 80;
        private uint _initDeadListShaderResId = ResourceManager.NullResource;
        public int ParticleSystemSizeInBytes => MaxCount * ParticleSizeInBytes;

        public void Init()
        {
            if (_initDeadListShaderResId == ResourceManager.NullResource)
            {
                string sourcePath = @"Resources\lib\particles\particle-dead-list-init.hlsl";
                string entryPoint = "main";
                string debugName = "particle-dead-list-init";
                var resourceManager = ResourceManager.Instance();
                _initDeadListShaderResId = resourceManager.CreateComputeShaderFromFile(sourcePath, entryPoint, debugName, null);
            }

            InitParticleBufferAndViews();
            InitDeadParticleIndices();
            InitAliveParticleIndices();
            InitIndirectArgBuffer();
            InitParticleCountConstBuffer();
        }

        private void InitParticleBufferAndViews()
        {
            var resourceManager = ResourceManager.Instance();
            int stride = ParticleSizeInBytes;
            var bufferData = Enumerable.Repeat(-10.0f, MaxCount * (stride / 4)).ToArray(); // init with negative lifetime other values doesn't matter
            ResourceManager.SetupStructuredBuffer(bufferData, stride * MaxCount, stride, ref ParticleBuffer);
            ResourceManager.CreateStructuredBufferUav(ParticleBuffer, UnorderedAccessViewBufferFlags.None, ref ParticleBufferUav);
            ResourceManager.CreateStructuredBufferSrv(ParticleBuffer, ref ParticleBufferSrv);
        }

        private const int ParticleIndexSizeInBytes = 8;

        private void InitDeadParticleIndices()
        {
            // init the buffer 
            var resourceManager = ResourceManager.Instance();
            ResourceManager.SetupStructuredBuffer(ParticleIndexSizeInBytes*MaxCount, ParticleIndexSizeInBytes, ref DeadParticleIndices);
            ResourceManager.CreateStructuredBufferUav(DeadParticleIndices, UnorderedAccessViewBufferFlags.Append, ref DeadParticleIndicesUav);
            
            // init counter of the dead list buffer (must be done due to uav binding)
            ComputeShader deadListInitShader = resourceManager.GetComputeShader(_initDeadListShaderResId);
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var csStage = deviceContext.ComputeShader;
            var prevShader = csStage.Get();
            var prevUavs = csStage.GetUnorderedAccessViews(0, 1);
            
            // set and call the init shader
            var reflectedShader = new ShaderReflection(resourceManager.GetComputeShaderBytecode(_initDeadListShaderResId));
            reflectedShader.GetThreadGroupSize(out int x, out int y, out int z);
            
            csStage.Set(deadListInitShader);
            csStage.SetUnorderedAccessView(0, DeadParticleIndicesUav, 0);
            int dispatchCount = MaxCount / (x > 0 ? x : 1);
            Log.Info($"particle system: maxcount {MaxCount}  dispatchCount: {dispatchCount} *64: {dispatchCount*64}");
            deviceContext.Dispatch(dispatchCount, 1, 1);
            
            // restore prev setup
            csStage.SetUnorderedAccessView(0, prevUavs[0]);
            csStage.Set(prevShader);
        }

        private void InitAliveParticleIndices()
        {
            var resourceManager = ResourceManager.Instance();
            ResourceManager.SetupStructuredBuffer(ParticleIndexSizeInBytes*MaxCount, ParticleIndexSizeInBytes, ref AliveParticleIndices);
            ResourceManager.CreateStructuredBufferUav(AliveParticleIndices, UnorderedAccessViewBufferFlags.Counter, ref AliveParticleIndicesUav);
            ResourceManager.CreateStructuredBufferSrv(AliveParticleIndices, ref AliveParticleIndicesSrv);
        }

        private void InitIndirectArgBuffer()
        {
            var resourceManager = ResourceManager.Instance();
            int sizeInBytes = 16;
            ResourceManager.SetupIndirectBuffer(sizeInBytes, ref IndirectArgsBuffer);
            ResourceManager.CreateBufferUav<uint>(IndirectArgsBuffer, Format.R32_UInt, ref IndirectArgsBufferUav);
        }
        
        private void InitParticleCountConstBuffer()
        {
            ResourceManager.SetupConstBuffer(Vector4.Zero, ref ParticleCountConstBuffer);
            ParticleCountConstBuffer.DebugName = "ParticleCountConstBuffer";
        }
    }
}