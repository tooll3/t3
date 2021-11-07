using SharpDX;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_eff2ffff_dc39_4b90_9b1c_3c0a9a0108c6
{
    public class MouseInput : Instance<MouseInput>
    {
        [Output(Guid = "CDC87CE1-FAB8-4B96-9137-9965E064BFA3", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<Vector2> Position = new Slot<Vector2>();

        // [Output(Guid = "4BCD7A08-0E47-4DD8-BB67-8E1B3793321E", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        // public readonly Slot<Int3> MousePressed = new Slot<Int3>();

        [Output(Guid = "78CAABCF-9C3B-4E50-9D80-BDCBABAEB003", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> IsLeftButtonDown = new Slot<bool>();

        /// <summary>
        /// This needs to be called from Imgui or Program 
        /// </summary>
        public static void Set(Vector2 newPosition, bool isLeftButtonDown)
        {
            _lastPosition = newPosition;
            _isLeftButtonDown = isLeftButtonDown;
        }

        private static Vector2 _lastPosition = Vector2.Zero;
        private static bool _isLeftButtonDown;
        
        public MouseInput()
        {
            Position.UpdateAction = Update;
            IsLeftButtonDown.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            Position.Value = _lastPosition;
            IsLeftButtonDown.Value = _isLeftButtonDown;
        }
        
        [Input(Guid = "49775CC2-35B7-4C9F-A502-59FE8FBBE2A7")]
        public readonly InputSlot<bool> DoUpdate = new InputSlot<bool>();
    }
}
