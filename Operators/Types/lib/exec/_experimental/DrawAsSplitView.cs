using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_f19a9234_cd23_4229_a794_aa9d97ad8027
{
    public class DrawAsSplitView : Instance<DrawAsSplitView>
    {
        [Output(Guid = "65456554-355b-41a3-893e-960d28113f53")]
        public readonly Slot<Command> Output = new Slot<Command>();


        [Input(Guid = "a3929303-170b-496a-b8e0-fc5f604a0ec7")]
        public readonly MultiInputSlot<Command> Commands = new MultiInputSlot<Command>();

        [Input(Guid = "6074ddd7-fc1f-4ebc-8511-f6003c75f11d")]
        public readonly InputSlot<float> WidthFactor = new InputSlot<float>();

        [Input(Guid = "3fd4d565-f4b9-4592-a544-2250ab3d16ab")]
        public readonly InputSlot<int> Count = new InputSlot<int>();

        [Input(Guid = "92677DCA-DB04-43B9-84FD-6AD485DEB209")]
        public readonly InputSlot<object> CameraRef = new InputSlot<object>();
    }
}

