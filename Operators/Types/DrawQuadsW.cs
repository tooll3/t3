using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ffc0a7ed_fe61_4188_8db9_0b0f07c6b981
{
    public class DrawQuadsW : Instance<DrawQuadsW>
    {
        [Output(Guid = "65bf6652-0187-4c5f-8e1f-ccc4254b843b")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "a3543b5d-0ee1-47de-9ef3-7747d7f9903f")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "82230324-b1cd-41ba-8d03-933c939001ad")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "c25ab214-7fb3-4595-8740-96471df44905")]
        public readonly InputSlot<System.Numerics.Vector2> Stretch = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "6fb70409-1cdd-488d-b7bb-aeb8ffaf084c")]
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "b1a7da14-d6bd-4862-ad69-dfa7ae1cfbb8")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "a0d7fc98-590d-481a-83a2-8522a3053082")]
        public readonly InputSlot<float> WSpeed = new InputSlot<float>();

        [Input(Guid = "1dfc889d-ed5b-4e82-b29c-a1b9079b8fa8")]
        public readonly InputSlot<bool> ApplyPointOrientation = new InputSlot<bool>();

        [Input(Guid = "4be3c132-1318-426e-a2ed-9534110ca03f")]
        public readonly InputSlot<float> Rotate = new InputSlot<float>();

        [Input(Guid = "c15a2562-824d-416a-91fd-6bab0380ff0f")]
        public readonly InputSlot<System.Numerics.Vector3> RotateAxis = new InputSlot<System.Numerics.Vector3>();
        
        [Input(Guid = "4256223c-ed88-4263-90f0-96cbc6da84d2")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture_ = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "a6069687-171d-417b-a746-7557486204bc")]
        public readonly InputSlot<int> BlendMod = new InputSlot<int>();

        [Input(Guid = "b83c1c1d-fef9-48d0-bf6b-da5b4ad8b094")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();

        [Input(Guid = "c080dd2e-3043-4f3f-a8fe-19cdce6ca5b4")]
        public readonly InputSlot<T3.Core.DataTypes.Gradient> Gradient = new InputSlot<T3.Core.DataTypes.Gradient>();

        [Input(Guid = "9645b08f-ed92-4b82-8090-0a31162e83fb")]
        public readonly InputSlot<T3.Core.Animation.Curve> Scale = new InputSlot<T3.Core.Animation.Curve>();

    }
}

