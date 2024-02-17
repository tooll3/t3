using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.point
{
	[Guid("c7f87607-dea2-4250-bd51-65fb6e12872d")]
    public class DrawQuadsExample : Instance<DrawQuadsExample>
    {
        [Output(Guid = "a50533ca-334d-412e-8b63-7bea7932466d")]
        public readonly Slot<Command> Output = new();


    }
}

