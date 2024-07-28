using SharpDX;
using System.IO.Ports;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d69e0f2e_8fe2_478b_ba4e_2a55a92670ae
{
    public class Gamepad : Instance<Gamepad>
    {
        [Output(Guid = "d4c0b5fc-11b7-497e-b59d-fed8ac8e2da2", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> Start = new Slot<bool>();

        [Output(Guid = "50baff6e-89bc-4c5f-9018-d6f680db1333", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> Back = new Slot<bool>();

        [Output(Guid = "68f0c486-5922-4b76-9d4e-70d80bf491bf", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<System.Numerics.Vector2> LeftThumb = new();

        [Output(Guid = "2ccb0b7f-a26e-4db2-b527-7d238c9a9589", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> LeftTrigger = new Slot<float>();

        [Input(Guid = "b37ab751-1e41-4e58-9e27-5ac9052662d8")]
        public readonly InputSlot<int> Index = new InputSlot<int>();

        private int currentIndex = -1;
        private SharpDX.XInput.Controller currentController;

        public Gamepad()
        {
            Start.UpdateAction = Update;
            Back.UpdateAction = Update;
            LeftThumb.UpdateAction = Update;
            LeftTrigger.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var index = Index.GetValue(context);
            if (index != currentIndex)
            {
                // FIXME : handle multi controller
                this.currentController = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.One);
                this.currentIndex = index;
            }

            var state = XInputGamepad.GetState(this.currentController);

            Start.Value = state.Start;
            Back.Value = state.Back;
            LeftThumb.Value = new System.Numerics.Vector2(state.LeftThumb.X, state.LeftThumb.Y);
            LeftTrigger.Value = state.LeftTrigger;


            //Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
        }
    }
}
