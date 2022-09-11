using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Rendering;
using Utilities = T3.Core.Utilities;
using Vector4 = System.Numerics.Vector4;


namespace T3.Operators.Types.Id_dee8f2de_5cbd_4ca7_9449_d6c74197546e
{
    public class GetTextureFromContext : Instance<GetTextureFromContext>
    {
        [Output(Guid = "C7CAC361-00D9-48D4-BE48-311551F3D449", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();

        public GetTextureFromContext()
        {
            Output.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var id = Id.GetValue(context);
            
            context.ContextTextures.TryGetValue(id, out var texture);
            Output.Value = texture;
        }
        
        [Input(Guid = "2986cf49-0ca3-46ce-8725-34f3c9d3c116")]
        public readonly InputSlot<string> Id = new InputSlot<string>();
        
    }
}