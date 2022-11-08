using System;
using System.Numerics;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Interfaces;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1dd15180_241b_47bd_85be_260fe57b19e9
{
    public class BuildTextSprites : Instance<BuildTextSprites>
    {

        [Output(Guid = "461a73c9-c445-4a6c-9fca-a3922b5066e9")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> TextPoints = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Output(Guid = "28f3ab88-a2c0-4116-99b2-0ae1e3714671")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> TextSprites = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Output(Guid = "ddf2dd98-f0ec-4f90-be88-6ca7a6950a52")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> FontAtlas = new Slot<SharpDX.Direct3D11.Texture2D>();
        
        
        [Input(Guid = "f057fdce-2057-498b-be54-e66c1fdc561b")]
        public readonly InputSlot<string> InputText = new InputSlot<string>();

        [Input(Guid = "83e2498f-2453-480e-b8eb-087f5272d1ce")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "4baed660-b64b-412f-aec1-2a34d33ebb9e")]
        public readonly InputSlot<System.Numerics.Vector4> Shadow = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "b2f008c6-c932-40a1-8a4e-7bdf6abe639d")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "1f49aa9e-14a4-4e41-92a8-d46a3e2f20e7")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "fffb2cd8-517f-4b90-a714-9aabc6bc4a07")]
        public readonly InputSlot<float> LineHeight = new InputSlot<float>();

        [Input(Guid = "a8ea5d2c-e1a7-47ff-96a7-26d789e64474")]
        public readonly InputSlot<int> VerticalAlign = new InputSlot<int>();

        [Input(Guid = "c34dca20-6f52-4a73-82f1-1c496d1a501b")]
        public readonly InputSlot<int> HorizontalAlign = new InputSlot<int>();

        [Input(Guid = "70ad312d-3beb-4333-8057-2228803d06a2")]
        public readonly InputSlot<float> Spacing = new InputSlot<float>();

        [Input(Guid = "05e06f3b-e703-40db-a6b3-137a31a71832")]
        public readonly InputSlot<string> FontPath = new InputSlot<string>();

        [Input(Guid = "54476a37-da36-4618-aecf-c4f4f04ca4c1")]
        public readonly InputSlot<SharpDX.Direct3D11.CullMode> CullMode = new InputSlot<SharpDX.Direct3D11.CullMode>();

        [Input(Guid = "a3ee71e3-7fc9-4082-99f3-62f83c4a8ae4")]
        public readonly InputSlot<bool> EnableZTest = new InputSlot<bool>();

        [Input(Guid = "f396fe59-9954-431a-a87f-ef1910c83b47")]
        public readonly InputSlot<float> TransitionProgress = new InputSlot<float>();

        [Input(Guid = "1a4764ee-dca0-48ae-ad67-edaac31ea689")]
        public readonly InputSlot<float> TransitionSpread = new InputSlot<float>();

        [Input(Guid = "c51e8ff6-2295-4759-8ed7-13508feb88b2")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> ColorTransition = new InputSlot<T3.Core.DataTypes.Gradient>();

        [Input(Guid = "2563d079-7c77-4bb6-a9a5-35b85de3b4db")]
        public readonly InputSlot<System.Numerics.Vector3> Movement = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "f5c0c8a0-dd70-42b2-bbf5-617d07554462")]
        public readonly InputSlot<float> RandomizeTiming = new InputSlot<float>();

        [Input(Guid = "15e604cd-0d05-41e9-adbb-b0371e15817b")]
        public readonly InputSlot<float> RandomizeMovement = new InputSlot<float>();

        [Input(Guid = "a77de4f5-4f97-4354-b97b-490184161be2")]
        public readonly InputSlot<float> Seed = new InputSlot<float>();

    }
}

