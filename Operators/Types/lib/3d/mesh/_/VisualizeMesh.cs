using T3.Core.DataTypes;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f8b12b4f_c10b_4e8b_9a69_344dbe8a063e
{
    public class VisualizeMesh : Instance<VisualizeMesh>
    {
        [Output(Guid = "5aa00627-91e7-449a-90e5-9f6df0d3eb14")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "ae443ba9-13b9-4692-97c4-a22d7acafcd4")]
        public readonly InputSlot<MeshBuffers> Mesh = new InputSlot<MeshBuffers>();

    }
}

