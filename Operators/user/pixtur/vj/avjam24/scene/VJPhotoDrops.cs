using System.Runtime.InteropServices;
using System;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.vj.avjam24.scene
{
    [Guid("e443fb22-4186-4dd4-b455-1b6d76e666e9")]
    public class VJPhotoDrops : Instance<VJPhotoDrops>
    {
        [Output(Guid = "7c0fe49d-c84e-4cb2-b561-ba9d9947520b")]
        public readonly Slot<Command> Output = new Slot<Command>();

    }
}

