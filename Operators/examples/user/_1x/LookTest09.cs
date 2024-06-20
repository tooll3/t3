using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("61449e42-9321-4039-8cbb-6cdd78ebf1e2")]
    public class LookTest09 : Instance<LookTest09>
    {

        [Output(Guid = "1e52b779-6a2d-4352-aca5-7d4600820ba3")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

