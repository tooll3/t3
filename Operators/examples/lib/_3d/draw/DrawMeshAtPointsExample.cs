using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib._3d.draw
{
	[Guid("4f113e4a-eb27-4e40-8843-d15d54610f33")]
    public class DrawMeshAtPointsExample : Instance<DrawMeshAtPointsExample>
    {

        [Output(Guid = "823e0f6a-518b-46cd-a929-7e069fe653a7")]
        public readonly Slot<Texture2D> ImageOutput = new();


    }
}

