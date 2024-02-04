using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;
using T3.Core.Rendering.Material;

namespace user.pixtur.research.gltf
{
	[Guid("2fcdea21-18f1-4006-a2fe-aab40893fed8")]
    public class DrawScene : Instance<DrawScene>
,ICustomDropdownHolder
    {
        [Output(Guid = "3b00e1d6-f966-4b03-81fc-2291e0fa7dbf")]
        public readonly Slot<Command> Output = new();
        
        
        public DrawScene()
        {
            Output.UpdateAction = Update;
        }


        private void Update(EvaluationContext context)
        {
            Log.Debug("Here", this);
            _pbrMaterials = context.Materials;
        }

        [Input(Guid = "22ad6256-f741-4e8f-9a47-4b5b82e2cecf")]
        public readonly InputSlot<T3.Core.DataTypes.SceneSetup> Scene = new InputSlot<T3.Core.DataTypes.SceneSetup>();

        [Input(Guid = "d7b086be-46c5-4021-8e7a-571236495594")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> Mesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "dd74d2b9-8c91-4a2a-adca-5ca187d433a3")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "69151dfb-59cf-4618-8642-c9dc88260786")]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "42e7cc49-5102-4e74-8bea-c7698cb4abca")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> Culling = new InputSlot<SharpDX.Direct3D11.CullMode>();

        [Input(Guid = "121f0f29-36f3-4ef7-9ebf-d14ad65b16a2")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "d517b62c-0f11-4d76-a945-4dabb7d84d74")]
        public readonly InputSlot<bool> EnableZWrite = new InputSlot<bool>();

        [Input(Guid = "35d67171-b2e3-45af-9adc-5c6539319ce9")]
        public readonly InputSlot<float> AlphaCutOff = new InputSlot<float>();

        [Input(Guid = "49e57c71-cbd3-4f8a-9a96-8fb116c947b0")]
        public readonly InputSlot<int> FillMode = new InputSlot<int>();

        [Input(Guid = "a5ba01f8-5176-4f2f-aac3-ba7d8ede4c20")]
        public readonly InputSlot<SharpDX.Direct3D11.Filter> Filter = new InputSlot<SharpDX.Direct3D11.Filter>();

        [Input(Guid = "b17800d2-cdf3-4334-90a0-d10b8cc27445")]
        public readonly InputSlot<SharpDX.Direct3D11.TextureAddressMode> WrapMode = new InputSlot<SharpDX.Direct3D11.TextureAddressMode>();

        [Input(Guid = "07089671-d27e-4eec-9719-8f6db7479b0b")]
        public readonly InputSlot<string> UseMaterialId = new InputSlot<string>();

        
        public string GetValueForInput(Guid inputId)
        {
            if (inputId != UseMaterialId.Input.InputDefinition.Id)
                return "Undefined input";

            return "Default";
        }

        public IEnumerable<string> GetOptionsForInput(Guid inputId)
        {
            yield return "Default";
            
            if(_pbrMaterials == null)
                yield break;

            foreach (var m in _pbrMaterials)
            {
                yield return string.IsNullOrEmpty(m.Name) ? "undefined" : m.Name;
            }
        }

        public void HandleResultForInput(Guid inputId, string result)
        {
            if (inputId != UseMaterialId.Input.InputDefinition.Id)
                return;
            
            UseMaterialId.SetTypedInputValue(result);
        }

        private List<PbrMaterial> _pbrMaterials;
    }
}

