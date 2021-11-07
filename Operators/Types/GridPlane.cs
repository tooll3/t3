using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_935e6597_3d9f_4a79_b4a6_600e8f28861e
{
    public class GridPlane : Instance<GridPlane>
    {
        [Output(Guid = "1eb82dc0-2e66-4c3c-a3e8-1b246886e59f")]
        public readonly Slot<T3.Core.Command> Output = new Slot<T3.Core.Command>();

        [Input(Guid = "3d0462a7-43bf-4564-a9a7-996e0b1902f2")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "c0f652b8-80fb-4bd2-b6cd-cfc459f9fcc5")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "39a74407-5447-45fd-8fc5-5f96bd8bbdfb")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();
    }
}

