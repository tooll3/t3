using SharpDX.Direct3D11;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c6014c28_c6ab_4b4e_b6bf_0cee92fb4b40
{
    public class ConvertEquirectangle : Instance<ConvertEquirectangle>
    {
        [Output(Guid = "000b79eb-b390-4b6b-9fdc-b99f12bc308d")]
        public readonly Slot<Texture2D> ColorBuffer = new Slot<Texture2D>();

        [Input(Guid = "032252e5-54dd-4636-a67f-f69c0806b172")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> X_negative = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "951a39bc-bfa7-4198-aa82-a24d9674bd0d")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> X_positive = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "d9a4bc23-a372-4030-9bca-856110cc5ce3")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Z_positive = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "d666ca81-bb4a-4f87-b453-fb56b02cdc23")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Z_negative = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "eb8d75f6-95b4-43c7-9a0c-e57730401c70")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Y_positive = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "75e6cd61-d764-49d7-a24d-adea0e84e252")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Y_negative = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "07d45e2f-75dd-455c-b8fe-b96ab2f830a2")]
        public readonly InputSlot<T3.Core.DataTypes.Vector.Int2> Resolution = new InputSlot<T3.Core.DataTypes.Vector.Int2>();

        [Input(Guid = "9d2ab882-14ec-4f05-a125-a12e87d53c76")]
        public readonly InputSlot<SharpDX.DXGI.Format> TextureFormat = new InputSlot<SharpDX.DXGI.Format>();

    }
}

