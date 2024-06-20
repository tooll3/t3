using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("69273e39-c475-47da-8882-e60fd4ea74f2")]
    public class LookTest12 : Instance<LookTest12>
    {

        [Output(Guid = "f3473eb5-39cf-404e-b4d8-bdd7950c0a80")]
        public readonly Slot<Texture2D> Output = new Slot<Texture2D>();


    }
}

