using System;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e44ae9b6_cd56_4224_8a5d_118eda4cd3f4
{
    public class ParticleSimulation2 : Instance<ParticleSimulation2>
    {

        [Output(Guid = "51b9c6bd-b7cc-48a4-979b-3febcac914c2")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Output(Guid = "3af47bd1-e47b-4c12-8660-d6264ea234a7")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> SimPoints = new Slot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "ba08e719-a1d1-4ac6-9c8c-076478a65a81")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> EmitPoints = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

        [Input(Guid = "7a320c47-14ed-4637-928b-25f87bd32c26")]
        public readonly InputSlot<bool> Emit = new InputSlot<bool>();

        [Input(Guid = "c6bdbb07-6996-4e5b-a058-37c9cbeca6fe")]
        public readonly InputSlot<bool> Reset = new InputSlot<bool>();

        [Input(Guid = "1eb32e4c-6785-4649-b0bf-7c70cddee619")]
        public readonly InputSlot<int> MaxParticleCount = new InputSlot<int>();

        [Input(Guid = "3cee4aa6-2ae3-4b4d-89c7-4c60c8f8b10e")]
        public readonly InputSlot<float> AgingRate = new InputSlot<float>();

        [Input(Guid = "85bb7b9f-f763-457c-aa0d-a08b15c31b50")]
        public readonly InputSlot<float> MaxAge = new InputSlot<float>();

        [Input(Guid = "a7350ba8-08be-4afc-92f1-d223ee9bcbeb", MappedType = typeof(SetWModes))]
        public readonly InputSlot<int> SetWTo = new InputSlot<int>();

        [Input(Guid = "9642f5c6-5ad2-4d35-a5ed-a3fde10817ae")]
        public readonly InputSlot<float> Speed = new InputSlot<float>();

        [Input(Guid = "5a61994e-42c7-47e7-b0a5-5beb48f4a34b")]
        public readonly InputSlot<float> Drag = new InputSlot<float>();

        [Input(Guid = "e9f068dd-9bd9-4b1c-9122-f78df0ec18b9")]
        public readonly InputSlot<bool> SetInitialVelocity = new InputSlot<bool>();

        [Input(Guid = "38392c86-4a1b-4b6f-ac32-26920a73e1e2")]
        public readonly InputSlot<float> InitialVelocity = new InputSlot<float>();

        [Input(Guid = "c41d9633-1397-4602-a5f8-7808c3d63108")]
        public readonly MultiInputSlot<T3.Core.DataTypes.ParticleSystem> ParticleEffects = new MultiInputSlot<T3.Core.DataTypes.ParticleSystem>();

        [Input(Guid = "4b0ccec5-b72e-4834-80d4-77225f30d2a9")]
        public readonly InputSlot<float> OrientTowardsVelocity = new InputSlot<float>();
        
        private enum SetWModes {
            KeepOriginal,
            Age,
            Speed,
        }
    }
}

