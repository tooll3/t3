using System;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Core.Rendering.Material;
using T3.Core.Resource;
using Utilities = T3.Core.Utils.Utilities;
using Vector4 = System.Numerics.Vector4;


namespace T3.Operators.Types.Id_0ed2bee3_641f_4b08_8685_df1506e9af3c
{
    public class SetMaterial : Instance<SetMaterial>
    {
        [Output(Guid = "d80e1028-a48d-4171-8c8c-e6856bd2143d")]
        public readonly Slot<Command> Output = new();

        public SetMaterial()
        {
            Output.UpdateAction = Update;
        }


        private void Update(EvaluationContext context)
        {

            var parameterBufferNeedsUpdate = BaseColor.DirtyFlag.IsDirty ||
                              EmissiveColor.DirtyFlag.IsDirty ||
                              Roughness.DirtyFlag.IsDirty ||
                              Specular.DirtyFlag.IsDirty ||
                              Metal.DirtyFlag.IsDirty ||
            _pbrMaterial == null;
            
            _pbrMaterial ??= new PbrMaterial();
            
            if (parameterBufferNeedsUpdate)
            {
                _pbrMaterial.Parameters.BaseColor = BaseColor.GetValue(context);
                _pbrMaterial.Parameters.EmissiveColor = EmissiveColor.GetValue(context);
                _pbrMaterial.Parameters.Roughness = Roughness.GetValue(context);
                _pbrMaterial.Parameters.Specular = Specular.GetValue(context);
                _pbrMaterial.Parameters.Metal = Metal.GetValue(context);

                _pbrMaterial.UpdateParameterBuffer();
            }

            UpdateSrv(BaseColorMap, context, ref _pbrMaterial.AlbedoColorSrv, PbrMaterial.DefaultAlbedoColorSrv);
            UpdateSrv(NormalMap, context, ref _pbrMaterial.NormalSrv, PbrMaterial.DefaultNormalSrv);
            UpdateSrv(EmissiveColorMap, context, ref _pbrMaterial.EmissiveColorSrv, PbrMaterial.DefaultEmissiveColorSrv);
            UpdateSrv(RoughnessMetallicOcclusionMap, context, ref _pbrMaterial.RoughnessMetallicOcclusionSrv, PbrMaterial.DefaultRoughnessMetallicOcclusionSrv);

            var previousMaterial = context.PbrMaterial;
            context.PbrMaterial = _pbrMaterial;

            SubTree.GetValue(context);
            context.PbrMaterial = previousMaterial;
        }

        private void UpdateSrv(InputSlot<Texture2D> textureInputSlot, EvaluationContext context, ref ShaderResourceView currentSrv, ShaderResourceView defaultSrv)
        {
            var textureChanged = textureInputSlot.DirtyFlag.IsDirty;
            var needsUpdate = textureChanged || currentSrv == null;

            if (!needsUpdate)
                return;

            if(currentSrv != defaultSrv)
                Utilities.Dispose(ref currentSrv);

            var changedTexture = textureInputSlot.GetValue(context);

            if (changedTexture == null || changedTexture.IsDisposed)
            {
                currentSrv = defaultSrv;
                return;
            }

            try
            {
                var srv = new ShaderResourceView(ResourceManager.Device, changedTexture);
                currentSrv = srv;
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to create SRV for {textureInputSlot.Input.Name} texture {e.Message}", this);
                currentSrv = defaultSrv;
            }
        }

        private PbrMaterial _pbrMaterial;

        
        [Input(Guid = "2a585a23-b60c-4c8b-8cfa-9ab2a8b04c7a")]
        public readonly InputSlot<Command> SubTree = new();

        [Input(Guid = "9FF5ADE2-CFA7-4616-AD89-356F3248E04F")]
        public readonly InputSlot<Vector4> BaseColor = new();

        [Input(Guid = "0EB51DF1-570A-4AC6-BAE6-5E03D6E66CEB")]
        public readonly InputSlot<Texture2D> BaseColorMap = new();

        [Input(Guid = "2C91C306-1815-4B22-A70F-746232F024D7")]
        public readonly InputSlot<Vector4> EmissiveColor = new();

        [Input(Guid = "6D859756-0243-42C5-973D-6DE2DCDC5609")]
        public readonly InputSlot<Texture2D> EmissiveColorMap = new();
        
        [Input(Guid = "9D5CA726-28B0-4F3D-A5AA-F0AE3E2043A9")]
        public readonly InputSlot<float> Specular = new();

        [Input(Guid = "E14DCC43-7C18-4ED7-AD39-DFEAB10E3D33")]
        public readonly InputSlot<float> Roughness = new();

        [Input(Guid = "108FF533-F205-4989-B894-ACF48E3CC1DB")]
        public readonly InputSlot<float> Metal = new();

        [Input(Guid = "600BBC24-6B3A-4AC4-9CEB-702E71C839E9")]
        public readonly InputSlot<Texture2D> NormalMap = new();

        [Input(Guid = "C8003FBD-C6CE-440C-9F1F-6B15B5EE5274")]
        public readonly InputSlot<Texture2D> RoughnessMetallicOcclusionMap = new();

    }
}