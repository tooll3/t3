using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_72e627e9_f570_4936_92b1_b12ed8d6004e
{
    public class EdgeRepeat : Instance<EdgeRepeat>
    {
        [Output(Guid = "f2e7625f-7918-4b0f-8b51-4304dde13bc6")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> TextureOutput = new Slot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "22b41600-7d42-44ad-8e94-3819f8d24964")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Image = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "35e6faff-7e13-4fa8-8aca-32efc02f29e0")]
        public readonly InputSlot<System.Numerics.Vector4> Fill = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "b9859a88-4dd1-4221-9884-0a6e8cec9da4")]
        public readonly InputSlot<System.Numerics.Vector4> Background = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "549020f5-e405-4f78-a973-9e9fb3c9c25e")]
        public readonly InputSlot<System.Numerics.Vector4> LineColor = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "fb9e226b-866a-4228-bad9-bc7908bfc442")]
        public readonly InputSlot<System.Numerics.Vector2> Center = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "b77a3329-c938-4dd3-bf3b-8774db187ef5")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "b465481c-8b5d-4c6d-a659-6f51135b86fa")]
        public readonly InputSlot<float> Rotation = new InputSlot<float>();

        [Input(Guid = "0f597b6c-eec8-4b06-92de-28f7cbe2243a")]
        public readonly InputSlot<float> PingPong = new InputSlot<float>();

        [Input(Guid = "881e04f7-1014-4e87-81c9-f4791086e57d")]
        public readonly InputSlot<float> LineThickness = new InputSlot<float>();

        [Input(Guid = "3acc47a3-83a8-4994-8456-e365595ed38c")]
        public readonly InputSlot<float> SmoothGradient = new InputSlot<float>();

        [Input(Guid = "c30a5d00-681c-490d-80d4-d58577cd65db")]
        public readonly InputSlot<SharpDX.Size2> Resolution = new InputSlot<SharpDX.Size2>();

        [Input(Guid = "294af91e-65f2-4e2c-8bd2-0896019f8e65")]
        public readonly InputSlot<float> Bias = new InputSlot<float>();
    }
}

