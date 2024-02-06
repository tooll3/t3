using System;
using System.Diagnostics.CodeAnalysis;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Operators.Types.Id_6e32756e_4267_47f1_bad0_56ee8f58b070
{
    /// <summary>
    /// Draws the nodes defined in the scene setup by dispatching a series of draw calls.
    /// To improve performance we:
    /// - set shaders once
    /// - set shared constant buffers (transform, fog, etc) only onec
    /// - only update material related SRVs
    /// - only only clean-up once
    ///
    /// LATER optimizations could include:
    /// - Using point references for sub component manipulation of color and transform
    /// - Using point references for instancing
    /// - Sorting draw order to draw batches of matching materials, meshes etc points.  
    /// 
    /// </summary>
    public class _DispatchSceneDraws : Instance<_DispatchSceneDraws>
    {
        [Output(Guid = "bda1583a-f5ea-4c3b-ae7d-0bac614d29ec")]
        public readonly Slot<Command> Output = new();

        public _DispatchSceneDraws()
        {
            Output.UpdateAction = Update;
        }
        
        private void Update(EvaluationContext context)
        {
            var device = ResourceManager.Device;
            var deviceContext = device.ImmediateContext;
            var vsStage = deviceContext.VertexShader;
            var psStage = deviceContext.PixelShader;

            // Keep current state
            _prevConstantBuffers = vsStage.GetConstantBuffers(0, _constantBuffers.Length);
            _prevShaderResourceViews = vsStage.GetShaderResources(0, ShaderResourceViews.Length);
            _prevSamplerStates = vsStage.GetSamplers(0, _samplerStates.Length);
            _prevVertexShader = vsStage.Get();
            _prevPixelShader = psStage.Get();
            
            // Shared resources shared by all draw calls
            var resourcesMissing = false;
            ConstantBuffers.GetValues(ref _constantBuffers, context);
            SamplerStates.GetValues(ref _samplerStates, context);
            
            resourcesMissing |= TryGetAndApplySrv(context, PrefilteredSpecular, PrefilteredSpecularIndex);
            resourcesMissing |= TryGetAndApplySrv(context, BrdfLookup, BrdfLookupIndex);

            // Draw...
            if (resourcesMissing)
            {
                Log.Warning("Resources missing", this);
            }
            else
            {
                TryDrawNodes(context, psStage,vsStage);
            }
            
            // Restore
            vsStage.Set(_prevVertexShader);
            vsStage.SetConstantBuffers(0, _prevConstantBuffers.Length, _prevConstantBuffers);
            vsStage.SetShaderResources(0, _prevShaderResourceViews.Length, _prevShaderResourceViews);
            
            psStage.Set(_prevPixelShader);
            psStage.SetConstantBuffers(0, _prevConstantBuffers.Length, _prevConstantBuffers);
            psStage.SetShaderResources(0, _prevShaderResourceViews.Length, _prevShaderResourceViews);
            psStage.SetSamplers(0, _prevSamplerStates.Length, _prevSamplerStates);
        }
        
        [SuppressMessage("Performance", "CA1822:Mark members as static")]
        private void TryDrawNodes(EvaluationContext context, PixelShaderStage psStage, VertexShaderStage vsStage)
        {
            var sceneSetup = SceneSetup.GetValue(context);
            if (sceneSetup?.DrawDispatches == null)
            {
                Log.Warning("undefined or empty scene setup");
                return;
            }

            int index = 0;
            
            foreach (var dispatch in sceneSetup.DrawDispatches)
            {
                Log.Debug($"Trying to draw {index}...");
                
                var material = dispatch.Material ?? context.PbrMaterial;

                // Resources changing with each draw call
                var resourcesMissing = false;
                resourcesMissing |= TestAndSet(dispatch.MeshBuffers?.VertexBuffer?.Srv, MeshVerticesIndex);
                resourcesMissing |= TestAndSet(dispatch.MeshBuffers?.IndicesBuffer?.Srv, MeshIndicesIndex);
                
                resourcesMissing |= TestAndSet(material?.NormalSrv, NormalMapIndex);
                resourcesMissing |= TestAndSet(material?.AlbedoMapSrv, AlbedoMapIndex);
                resourcesMissing |= TestAndSet(material?.EmissiveMapSrv, EmissiveMapIndex);
                resourcesMissing |= TestAndSet(material?.RoughnessMetallicOcclusionSrv, RmoColorMapIndex);
                
                if (resourcesMissing)
                {
                    Log.Debug("Skipping draw call because we're missing some resources");
                    continue;
                }
                
                vsStage.SetShaderResources(0, ShaderResourceViews.Length, ShaderResourceViews);
                psStage.SetShaderResources(0, ShaderResourceViews.Length, ShaderResourceViews);

                Log.Debug($"Dispatching draw for {dispatch.VertexCount} vertices...");
                ResourceManager.Device.ImmediateContext.Draw(dispatch.VertexCount, dispatch.VertexStartIndex);
            }
        }
        
        
        private bool TryGetAndApplySrv(EvaluationContext context, InputSlot<ShaderResourceView> srvInput, int index)
        {
            var srv = srvInput.GetValue(context);
            ShaderResourceViews[index] = srv;
            return srv == null;
        }

        private bool TestAndSet(ShaderResourceView srv, int index)
        {
            ShaderResourceViews[index] = srv;
            return srv == null;
        }
        
        
        private const int MeshVerticesIndex = 0;
        private const int MeshIndicesIndex = 1;
        private const int AlbedoMapIndex = 2;
        private const int EmissiveMapIndex = 3;
        private const int RmoColorMapIndex = 4;
        private const int NormalMapIndex = 5;
        private const int PrefilteredSpecularIndex = 6;
        private const int BrdfLookupIndex = 7;
        private const int IndexCount = 8;

        private static readonly ShaderResourceView[] ShaderResourceViews = new ShaderResourceView[IndexCount]; 
        
        private Buffer[] _constantBuffers = Array.Empty<Buffer>();
        private SamplerState[] _samplerStates = Array.Empty<SamplerState>();

        private PixelShader _prevPixelShader;
        private VertexShader _prevVertexShader;
        private SamplerState[] _prevSamplerStates = Array.Empty<SamplerState>();
        private Buffer[] _prevConstantBuffers;
        private ShaderResourceView[] _prevShaderResourceViews;
        
        [Input(Guid = "DAD22148-B87F-439A-9219-785BEE63991C")]
        public readonly InputSlot<SceneSetup> SceneSetup = new();
        
        [Input(Guid = "7a9ae929-7001-42ef-b7f2-f2e03bbb7206")]
        public readonly InputSlot<VertexShader> VertexShader = new();
        
        [Input(Guid = "59864DA4-3658-4D7D-830E-6EF0D3CBB505")]
        public readonly InputSlot<PixelShader> PixelShader = new();
        
        [Input(Guid = "9571b16e-72d1-4544-aa98-8a08b63bb5f6")]
        public readonly MultiInputSlot<Buffer> ConstantBuffers = new();

        // [Input(Guid = "83fdb275-3018-46a9-b75e-e9ee3d8660f4")]
        // public readonly MultiInputSlot<ShaderResourceView> ShaderResources = new();

        // [Input(Guid = "ED6773A0-7666-4053-9415-AD76AAA2E1D5")]
        // public readonly InputSlot<ShaderResourceView> MeshVertices = new();
        //
        // [Input(Guid = "4FD00D02-A9A0-4BF7-B2B0-754BD904D1EB")]
        // public readonly InputSlot<ShaderResourceView> MeshIndices = new();
        //
        // [Input(Guid = "912F1A3E-534A-4712-AE01-27768FCE2696")]
        // public readonly InputSlot<ShaderResourceView> AlbedoColorMap = new();
        //
        // [Input(Guid = "30A2CA5D-0AAE-4DA5-BF6B-336526C46871")]
        // public readonly InputSlot<ShaderResourceView> EmissiveColorMap = new();
        //
        // [Input(Guid = "CED29224-57F5-4CB4-97B4-E03E9B35EAB5")]
        // public readonly InputSlot<ShaderResourceView> RmoColorMap = new();
        //
        // [Input(Guid = "C65AB173-FBD0-4960-AF05-620C5CF0404A")]
        // public readonly InputSlot<ShaderResourceView> NormalColorMap = new();

        [Input(Guid = "27405ECF-4994-4E67-8604-D86426345344")]
        public readonly InputSlot<ShaderResourceView> PrefilteredSpecular = new();

        [Input(Guid = "3C19533B-C59A-470B-8712-073E71813C0C")]
        public readonly InputSlot<ShaderResourceView> BrdfLookup = new();
        
        [Input(Guid = "60bae25c-64fe-40df-a2e6-a99297a92e0b")]
        public readonly MultiInputSlot<SamplerState> SamplerStates = new();        
    }
}