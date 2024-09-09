using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_1998f949_5c0a_4f39_82cf_b0bda31f7f21
{
    public class DrawSphereGizmo : Instance<DrawSphereGizmo>
    {
        [Output(Guid = "0b43d459-2c94-4d5e-a75a-61d38d93118b")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "15f73f4f-a4f1-4ff0-aba5-074ea2d328bc")]
        public readonly InputSlot<float> Radius = new InputSlot<float>();

        [Input(Guid = "a407fbe3-1f1b-4466-b4c2-511733046d00")]
        public readonly InputSlot<float> InnerRadius = new InputSlot<float>();

        [Input(Guid = "188c2244-e55a-4c55-be93-cccbd6fd4e41")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();


    }
}

