using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d38fe24e_be01_4a22_9595_b06bc906bf49
{
    public class ShadowPlane : Instance<ShadowPlane>
    {
        [Output(Guid = "e7622351-6afd-41dd-b9c7-bddc1e25e128")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "7a51aab1-7de2-4bc2-bfb6-bd32aca0381c")]
        public readonly InputSlot<Command> Command = new InputSlot<Command>();

        [Input(Guid = "5a2f2521-c521-48a9-99c9-3916413c7938")]
        public readonly InputSlot<float> PlaneSize = new InputSlot<float>();

        [Input(Guid = "16527748-fe55-4ced-b08d-6c94377c5563")]
        public readonly InputSlot<float> FOV = new InputSlot<float>();

        [Input(Guid = "2f90c70b-7979-4426-aad2-7e715e7c1691", MappedType = typeof(Resolutions))]
        public readonly InputSlot<int> Resolution = new InputSlot<int>();

        [Input(Guid = "f192118e-ea03-4233-a7b5-93f0fa380e93")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "7a330847-eb4d-4b18-8f55-d220351537a7")]
        public readonly InputSlot<float> BlurRadius = new InputSlot<float>();

        [Input(Guid = "ad589e97-e1db-4557-8a46-f6618f3c969b")]
        public readonly InputSlot<System.Numerics.Vector2> BlurDistribution = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "bcfc6e35-6427-46fd-9902-189e88651ebe")]
        public readonly InputSlot<System.Numerics.Vector3> Center = new InputSlot<System.Numerics.Vector3>();

        private enum Resolutions
        {
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
        }
        
    }
}

