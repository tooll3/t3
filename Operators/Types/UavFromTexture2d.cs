using System;
using SharpDX.Direct3D11;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_84e02044_3011_4a5e_b76a_c904d9b4557f
{
    public class UavFromTexture2d : Instance<UavFromTexture2d>
    {
        [Output(Guid = "{83D2DCFD-3850-45D8-BB1B-93FE9C9F4334}")]
        public readonly Slot<UnorderedAccessView> UnorderedAccessView = new Slot<UnorderedAccessView>();

        public UavFromTexture2d()
        {
            UnorderedAccessView.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            if (!Texture.DirtyFlag.IsDirty)
                return; // nothing to do

            var resourceManager = ResourceManager.Instance();
            Texture2D texture = Texture.GetValue(context);
            if (texture != null)
            {
                if (((int)texture.Description.BindFlags & (int)BindFlags.UnorderedAccess) > 0)
                {
                    UnorderedAccessView.Value?.Dispose();
                    UnorderedAccessView.Value = new UnorderedAccessView(resourceManager.Device, texture); // todo: create via resource manager
                }
                else
                {
                    Log.Warning("Trying to create an unordered access view for resource which doesn't have the uav bind flag set");
                }
            }
        }

        [Input(Guid = "{4A4F6830-1809-42C9-91EB-D4DBD0290043}")]
        public readonly InputSlot<Texture2D> Texture = new InputSlot<Texture2D>();
    }
}