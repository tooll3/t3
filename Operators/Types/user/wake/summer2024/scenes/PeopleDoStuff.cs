using System;
using System;
using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_37cb4087_463a_4790_b3f2_07971d3cf6c3
{
    public class PeopleDoStuff : Instance<PeopleDoStuff>
    {

        [Output(Guid = "b9da6afd-8569-49fc-926f-67005d720da4")]
        public readonly Slot<T3.Core.DataTypes.Command> Scene = new Slot<T3.Core.DataTypes.Command>();


        [Input(Guid = "555dcc25-3991-4f72-8668-0a96566a2137")]
        public readonly MultiInputSlot<bool> Input = new MultiInputSlot<bool>();

        [Input(Guid = "c068eed3-5cd3-4d33-96f7-8cf4454310f9")]
        public readonly MultiInputSlot<bool> Input2 = new MultiInputSlot<bool>();

    }
}

