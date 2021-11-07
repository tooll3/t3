using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ca4fe8c4_cf61_4196_84e4_d69dc8869a25
{
    public class GetPbrParameters : Instance<GetPbrParameters>
    {
        [Output(Guid = "3D2EBD10-2670-46B7-8F1A-9475A81A516D", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Buffer> PbrParameterBuffer = new Slot<Buffer>();

        [Output(Guid = "7C3D08E2-85E2-442A-9196-0E946571DA5A", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<ShaderResourceView> AlbedoColorMap = new Slot<ShaderResourceView>();

        [Output(Guid = "B6BAD091-131A-49F3-8ACC-7011A4919435", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<ShaderResourceView> EmissiveColorMap = new Slot<ShaderResourceView>();

        [Output(Guid = "B48F674B-2B5A-4501-992E-26E07A247DDF", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<ShaderResourceView> RoughnessSpecularMetallicOcclusionMap = new Slot<ShaderResourceView>();

        [Output(Guid = "B815BF49-0E44-4DB0-BF32-9C7D188D6AA2", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<ShaderResourceView> NormalMap = new Slot<ShaderResourceView>();

        [Output(Guid = "671F198D-4173-4FE9-AF5A-FCD5D0A71895", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<ShaderResourceView> BrdfLookupMap = new Slot<ShaderResourceView>();
        
        [Output(Guid = "AB644673-9EAA-4CEC-9663-FBFDC445D112", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Texture2D> PrefilteredSpecularMap = new Slot<Texture2D>();
        
        public GetPbrParameters()
        {
            PbrParameterBuffer.UpdateAction = Update;
            AlbedoColorMap.UpdateAction = Update;
            EmissiveColorMap.UpdateAction = Update;
            RoughnessSpecularMetallicOcclusionMap.UpdateAction = Update;
            NormalMap.UpdateAction = Update;
            BrdfLookupMap.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            PbrParameterBuffer.Value = context.PbrMaterialParams;
            AlbedoColorMap.Value = context.PbrMaterialTextures.AlbedoColorMap;
            EmissiveColorMap.Value = context.PbrMaterialTextures.EmissiveColorMap;
            RoughnessSpecularMetallicOcclusionMap.Value = context.PbrMaterialTextures.RoughnessSpecularMetallicOcclusionMap;
            NormalMap.Value = context.PbrMaterialTextures.NormalMap;
            BrdfLookupMap.Value = context.PbrMaterialTextures.BrdfLookUpMap;
            PrefilteredSpecularMap.Value = context.PrbPrefilteredSpecular;
        }
    }
}