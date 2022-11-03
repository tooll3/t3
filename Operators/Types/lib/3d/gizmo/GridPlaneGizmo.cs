using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_e5588101_5686_4b02_ab7d_e58199ba552e
{
    public class GridPlaneGizmo : Instance<GridPlaneGizmo>
    {
        [Output(Guid = "34f1eab4-9379-4b4e-a160-1bfed9103597", DirtyFlagTrigger = DirtyFlagTrigger.Always)]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "006f8d2e-40ee-4682-9f16-8037742e7987")]
        public readonly InputSlot<float> Size = new InputSlot<float>();

        [Input(Guid = "feae6a6f-5fd7-490d-a95a-2c8758ceb9bf")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();

        [Input(Guid = "60070155-b8ee-4d3d-ab04-c9765fe825a2")]
        public readonly InputSlot<System.Numerics.Vector3> Rotation = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "ec754488-a2f9-466d-8605-edf58677df39")]
        public readonly InputSlot<float> Scale = new InputSlot<float>();


    }
}

