using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
	[Guid("2fcb6afc-2d82-47a5-a6dd-39c85348b8c4")]
    public class SelectPointsExample : Instance<SelectPointsExample>
    {
        [Output(Guid = "015dd888-70a7-412c-a2da-daada267c1ae")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

