using SharpDX.Direct3D11;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_270c37b6_7633_4952_bade_a3ea2300583c
{
    public class GetRenderTargetReference : Instance<GetRenderTargetReference>
    {
        public GetRenderTargetReference()
        {
            Result.UpdateAction = Update;
        }    
        
        [Output(Guid = "d66c4005-309f-415f-b776-3bd786fdaaa8")]
        public readonly Slot<Texture2D> Result = new();
        
        private void Update(EvaluationContext context)
        {
            var colorTexture = ColorTexture.GetValue(context);
            Result.Value = colorTexture;
            
            var reference = TextureReference.GetValue(context);
            if (TextureReference.HasInputConnections && TextureReference != null)
            {
                reference.ColorTexture = colorTexture;
                reference.DepthTexture = DepthTexture.GetValue(context);
            }
        }

        [Input(Guid = "f8e86ae6-f5c8-4de1-a4ed-58b43c2fe7f5")]
        public readonly InputSlot<Texture2D> ColorTexture = new();
        
        [Input(Guid = "ADA9D5F5-A4F2-4E5B-BCD1-EDA8FD268579")]
        public readonly InputSlot<Texture2D> DepthTexture = new();

        
        [Input(Guid = "290CD1E8-4793-4A65-AD90-3578B4410973")]
        public readonly InputSlot<RenderTargetReference> TextureReference = new();


    }
}
