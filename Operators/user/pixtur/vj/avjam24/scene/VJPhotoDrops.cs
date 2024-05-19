using System;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e443fb22_4186_4dd4_b455_1b6d76e666e9
{
    public class VJPhotoDrops : Instance<VJPhotoDrops>
    {
        [Output(Guid = "7c0fe49d-c84e-4cb2-b561-ba9d9947520b")]
        public readonly Slot<Command> Output = new Slot<Command>();

    }
}

