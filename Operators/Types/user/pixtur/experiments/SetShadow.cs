using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_facb7925_176a_4eae_bedc_cdbf532ff6ff
{
    public class SetShadow : Instance<SetShadow>
    {
        [Output(Guid = "a0a1b038-8637-45af-89b5-dcef99f872f7")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "be6dc055-d4c8-4c75-a084-12c22a268034")]
        public readonly InputSlot<T3.Core.DataTypes.Command> Command = new InputSlot<T3.Core.DataTypes.Command>();

        [Input(Guid = "6196074f-4770-4e81-812a-012fcaab207b")]
        public readonly InputSlot<System.Numerics.Vector3> LightPosition = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "76a6a0e4-168d-4172-9946-e843df9d0ca0")]
        public readonly InputSlot<System.Numerics.Vector3> LightDirection = new InputSlot<System.Numerics.Vector3>();

    }
}

