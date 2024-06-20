using System.Runtime.InteropServices;
using System;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.dx11.tex
{
	[Guid("daec568f-f7b4-4d81-a401-34d62462daab")]
    public class GetTextureSize : Instance<GetTextureSize>
    {
        [Output(Guid = "be16d5d3-4d21-4d5a-9e4c-c7b2779b6bdc")]
        public readonly Slot<Int2> Size = new();

        [Output(Guid = "895C3BDD-38A8-4613-A8B2-503EC9D493C8")]
        public readonly Slot<System.Numerics.Vector2> SizeFloat = new();

        [Output(Guid = "E54A3185-2E19-466B-9A1E-52A05A947FCD")]
        public readonly Slot<int> TotalSize = new();

        [Output(Guid = "209BF938-E317-4F9C-8906-265C2AFAE1E5")]
        public readonly Slot<bool> IsTextureValid = new ();


        public GetTextureSize()
        {
            Size.UpdateAction = Update;
            SizeFloat.UpdateAction = Update;
            TotalSize.UpdateAction = Update;
            IsTextureValid.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var texture = Texture.GetValue(context);
            IsTextureValid.Value = texture != null;
            
            var overrideResolution = OverrideSize.GetValue(context);
            
            // Log.Debug("is texture valid: " + IsTextureValid, this);
            var useContextResolution = overrideResolution.Width < 0 || overrideResolution.Height < 0;
            // Log.Debug(" UseContextResolution:" + useContextResolution);
            
            if (!useContextResolution && overrideResolution.Width > 0 && overrideResolution.Height > 0)
            {
                //Log.Debug($" use override size: {overrideResolution}", this);
                Size.Value = overrideResolution;
            }
            else if (texture != null)
            {
                // Log.Debug(" texture is none", this);
                try
                {
                    Size.Value = new Int2(texture.Description.Width, texture.Description.Height);
                    //Log.Debug($" use texture size: {Size.Value} {useContextResolution}", this);
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to get texture description: " + e.Message, this);
                }
            }
            else
            {
                useContextResolution = true;
            }
            
            if (useContextResolution)
            {
                //Log.Debug($"Set to animated resolution to : {context.RequestedResolution}", this);
                Size.Value = context.RequestedResolution;
                Size.DirtyFlag.Trigger =      DirtyFlagTrigger.Animated;
                SizeFloat.DirtyFlag.Trigger = DirtyFlagTrigger.Animated;
                TotalSize.DirtyFlag.Trigger = DirtyFlagTrigger.Animated;
                IsTextureValid.DirtyFlag.Trigger = DirtyFlagTrigger.Animated;
            }
            else
            {
                //Log.Debug("Set to none", this);
                Size.DirtyFlag.Trigger =      DirtyFlagTrigger.None;
                SizeFloat.DirtyFlag.Trigger = DirtyFlagTrigger.None;
                TotalSize.DirtyFlag.Trigger = DirtyFlagTrigger.None;
                IsTextureValid.DirtyFlag.Trigger = DirtyFlagTrigger.None;
            }

            SizeFloat.Value = new System.Numerics.Vector2(Size.Value.Width, Size.Value.Height);
            TotalSize.Value = Size.Value.Width * Size.Value.Height;
            
            Size.DirtyFlag.Clear();
            TotalSize.DirtyFlag.Clear();
            SizeFloat.DirtyFlag.Clear();
            Texture.DirtyFlag.Clear();
            IsTextureValid.DirtyFlag.Clear();
        }
        

        [Input(Guid = "8b15d8e1-10c7-41e1-84db-a85e31e0c909")]
        public readonly InputSlot<Texture2D> Texture = new();

        [Input(Guid = "52b2f067-5619-4d8d-a982-58668a8dc6a4")]
        public readonly InputSlot<Int2> OverrideSize = new();
    }
}