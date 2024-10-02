using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using T3.Core.Rendering.Material;
using T3.Core.Resource;
using Utilities = T3.Core.Utils.Utilities;
using Vector4 = System.Numerics.Vector4;


namespace T3.Operators.Types.Id_c1201ee1_c6aa_4d1d_82af_6239cad9def4
{
    public class SetRenderPass : Instance<SetRenderPass>
    {
        [Output(Guid = "ef38e0f6-4bcb-44f4-bf61-98b3039e21e9")]
        public readonly Slot<Command> Output = new Slot<Command>();

        public SetRenderPass()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var id = Id.GetValue(context);
            var tex = Texture.GetValue(context) ?? PbrContextSettings.WhitePixelTexture;
            var hadPreviousTexture = context.ContextTextures.TryGetValue(id, out var previousTexture);
            context.ContextTextures[id] = tex;
            
            SubTree.GetValue(context);
            if (hadPreviousTexture)
            {
                context.ContextTextures[id] = previousTexture;
            }
            else
            {
                context.ContextTextures.Remove(id);
            }
        }

        [Input(Guid = "5c82a1e8-d91f-4777-9c9c-0b4509dd8150")]
        public readonly InputSlot<Command> SubTree = new InputSlot<Command>();

        [Input(Guid = "56bfc2ec-1455-41aa-a3b8-ffe3ead75941")]
        public readonly InputSlot<string> Id = new InputSlot<string>();
        
        [Input(Guid = "2623ed48-b145-4942-9730-a5936a104def")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();

    }
}