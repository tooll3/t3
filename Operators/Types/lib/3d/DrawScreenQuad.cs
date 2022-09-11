using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_5a2752e8_95ae_4d76_b903_1f52ef43bcdc
{
    public class DrawScreenQuad : Instance<DrawScreenQuad>
    {
        [Output(Guid = "3c8116a2-2686-41ba-8bfd-d1b3fb929b02")]
        public readonly Slot<T3.Core.Command> Output = new Slot<T3.Core.Command>();

        [Input(Guid = "92c66734-dce9-402a-95f6-cde0e58bf32f")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "4e8fecd0-00ca-404e-a9d4-1cb0d3e044f1")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "a680706a-3e0f-4b93-9953-05df8d32109a")]
        public readonly InputSlot<float> Width = new InputSlot<float>();

        [Input(Guid = "4ca612b7-a899-4567-98e4-8b6e96d5f251")]
        public readonly InputSlot<float> Height = new InputSlot<float>();

        [Input(Guid = "60cef5fb-b22a-4fa0-83bb-093d0f1d5b56")]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "6a91ba46-257d-4c3c-94c5-72cbc06ee816")]
        public readonly InputSlot<bool> EnableDepthTest = new InputSlot<bool>();

        [Input(Guid = "2680baf9-3dbd-4ade-b109-19b3b0f1d40f")]
        public readonly InputSlot<bool> EnableDepthWrite = new InputSlot<bool>();

        [Input(Guid = "6fd86a21-d06c-4edb-bbe2-39d7411e46d1")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new InputSlot<System.Numerics.Vector2>();
    }
}

