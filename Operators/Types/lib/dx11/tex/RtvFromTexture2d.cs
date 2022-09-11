using System;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_57a1ee33_702a_41ad_a17e_b43033d58638
{
    public class RtvFromTexture2d : Instance<RtvFromTexture2d>
    {
        [Output(Guid = "515E87C9-4CF8-4948-BA64-F6261F7FE5FC")]
        public readonly Slot<RenderTargetView> RenderTargetView = new Slot<RenderTargetView>();

        public RtvFromTexture2d()
        {
            RenderTargetView.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (!Texture.DirtyFlag.IsDirty)
                return; // nothing to do

            var resourceManager = ResourceManager.Instance();
            
            
            
            Texture2D texture = Texture.GetValue(context);
            if (texture != null)
            {
                if (((int)texture.Description.BindFlags & (int)BindFlags.RenderTarget) > 0)
                {
                    RenderTargetView.Value?.Dispose();
                    if ((texture.Description.OptionFlags & ResourceOptionFlags.TextureCube) != 0)
                    {
                        var rtvDesc = new RenderTargetViewDescription()
                                          {
                                              Dimension = RenderTargetViewDimension.Texture2DArray,
                                              Format = texture.Description.Format,
                                              Texture2DArray = new RenderTargetViewDescription.Texture2DArrayResource() 
                                                                   {
                                                                       ArraySize = 6,
                                                                       FirstArraySlice = 0,
                                                                       MipSlice = 0
                                                                   }                                                           
                                          };
                        //rtvDesc.Texture2DArray.MipSlice = 0;
                        RenderTargetView.Value = new RenderTargetView(resourceManager.Device, texture, rtvDesc);
                    }
                    else
                    {
                        RenderTargetView.Value = new RenderTargetView(resourceManager.Device, texture); // todo: create via resource manager
                    }
                }
                else
                {
                    Log.Warning("Trying to create an render target view for resource which doesn't have the rtv bind flag set");
                }
            }
        }

        [Input(Guid = "73CF7C5D-CF0C-49DB-91E8-DAFE812E0232")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();
    }
}