using System;
using SharpDX;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_daec568f_f7b4_4d81_a401_34d62462daab
{
    public class GetTextureSize : Instance<GetTextureSize>
    {
        [Output(Guid = "be16d5d3-4d21-4d5a-9e4c-c7b2779b6bdc", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<SharpDX.Size2> Size = new Slot<SharpDX.Size2>();

        [Output(Guid = "895C3BDD-38A8-4613-A8B2-503EC9D493C8", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<System.Numerics.Vector2> SizeFloat = new Slot<System.Numerics.Vector2>();

        [Output(Guid = "E54A3185-2E19-466B-9A1E-52A05A947FCD", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<int> TotalSize = new Slot<int>();

        public GetTextureSize()
        {
            Size.UpdateAction = Update;
            SizeFloat.UpdateAction = Update;
            //Size.DirtyFlag.Trigger = DirtyFlagTrigger.Always;
        }

        private void Update(EvaluationContext context)
        {
            var fallbackSize = OverrideSize.GetValue(context);

            var useContext = fallbackSize.Width < 0 || fallbackSize.Height < 0;

            var texture = Texture.GetValue(context);
            if (useContext)
            {
                Size.Value = context.RequestedResolution;
            }
            else if (fallbackSize.Width > 0 && fallbackSize.Height > 0)
            {
                Size.Value = fallbackSize;
            }
            else if (texture != null)
            {
                try
                {
                    Size.Value = new Size2(texture.Description.Width, texture.Description.Height);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to get texture description: " + e, SymbolChildId);
                }
            }
            else
            {
                Size.Value = context.RequestedResolution;
                //Size.Value = new Size2(0,0);
            }

            SizeFloat.Value = new System.Numerics.Vector2(Size.Value.Width, Size.Value.Height);
            TotalSize.Value = Size.Value.Width * Size.Value.Height;
        }

        private enum Modes
        {
            UseExplicit,
            UseRequestedFromContext,
            UseRequestedFromInputTexture,
        }

        [Input(Guid = "8b15d8e1-10c7-41e1-84db-a85e31e0c909")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "52b2f067-5619-4d8d-a982-58668a8dc6a4")]
        public readonly InputSlot<SharpDX.Size2> OverrideSize = new InputSlot<SharpDX.Size2>();
    }
}