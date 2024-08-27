using SharpDX;
using System;
using System.IO.Ports;
using System.Runtime.InteropServices;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_d69e0f2e_8fe2_478b_ba4e_2a55a92670ae
{
    public class Gamepad : Instance<Gamepad>
    {
        [Output(Guid = "d4c0b5fc-11b7-497e-b59d-fed8ac8e2da2", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> Start = new();

        [Output(Guid = "50baff6e-89bc-4c5f-9018-d6f680db1333", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> Back = new();

        [Output(Guid = "68f0c486-5922-4b76-9d4e-70d80bf491bf", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<System.Numerics.Vector2> LeftThumb = new();

        [Output(Guid = "2BA7BA57-4575-4F00-BAF1-7FA8D589C04F", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<System.Numerics.Vector2> RightThumb = new();
        
        [Output(Guid = "2ccb0b7f-a26e-4db2-b527-7d238c9a9589", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> LeftTrigger = new();

        [Output(Guid = "358AE1A6-26EE-4637-9B47-5DB294B0063B", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<float> RightTrigger = new();

        [Output(Guid = "87BA8362-03DD-4D8A-809A-2E9890FB8AA9", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> LeftShoulder = new();

        [Output(Guid = "C99D778E-9678-43CC-86C6-E9FA2A794507", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> RightShoulder = new();

        [Output(Guid = "54D386A7-0CDC-4119-A052-1D601721EB50", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> A = new();

        [Output(Guid = "1cbd54c0-8b9e-47d7-a35b-1323044bf21c", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> B = new();

        [Output(Guid = "fe0a089a-edc7-45fd-8447-f0b43d48a444", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> X = new();

        [Output(Guid = "5f0e04c9-7956-4805-ae4d-8227e875a92a", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> Y = new();

        [Output(Guid = "1829765c-3ec1-4750-b51b-610b7f50a95f", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> DPadLeft = new();

        [Output(Guid = "82f53f0b-869e-41c1-a5aa-a9bf7cd55b88", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> DPadRight = new();

        [Output(Guid = "8df9c334-238a-4542-93e0-a5baf44bf7a4", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> DPadUp = new();

        [Output(Guid = "5503c3a7-035f-4e4d-ac9a-dab5e0763c1a", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> DPadDown = new();
       

        public Gamepad()
        {
            Start.UpdateAction = Update;
            Back.UpdateAction = Update;

            LeftThumb.UpdateAction = Update;
            RightThumb.UpdateAction = Update;

            LeftTrigger.UpdateAction = Update;
            RightTrigger.UpdateAction = Update;

            LeftShoulder.UpdateAction = Update;
            RightShoulder.UpdateAction = Update;

            A.UpdateAction = Update;
            B.UpdateAction = Update;
            X.UpdateAction = Update;
            Y.UpdateAction = Update;

            DPadLeft.UpdateAction = Update;
            DPadRight.UpdateAction = Update;
            DPadUp.UpdateAction = Update;
            DPadDown.UpdateAction = Update;
 
        }

        private void Update(EvaluationContext context)
        {
            var index = Index.GetValue(context);
            var controllers = new SharpDX.XInput.Controller[4];
            controllers[0] = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.One);
            controllers[1] = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.Two);
            controllers[2] = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.Three);
            controllers[3] = new SharpDX.XInput.Controller(SharpDX.XInput.UserIndex.Four);

            // Ensure index is within valid range
            index = Math.Max(0, Math.Min(3, index));

            // Find the first connected controller, starting from the specified index
            for (int i = 0; i < 4; i++)
            {
                int controllerIndex = (index + i) % 4;
                if (controllers[controllerIndex].IsConnected)
                {
                    this._currentController = controllers[controllerIndex];
                    break;
                }
            }

            // If no controller is connected, set currentController to null
            if (this._currentController == null || !this._currentController.IsConnected)
            {
                this._currentController = null;
                return;
            }

            var state = XInputGamepad.GetState(this._currentController);

            Start.Value = state.Start;
            Back.Value = state.Back;

            LeftThumb.Value = new System.Numerics.Vector2(state.LeftThumb.X, state.LeftThumb.Y);
            RightThumb.Value = new System.Numerics.Vector2(state.RightThumb.X, state.RightThumb.Y);

            LeftTrigger.Value = state.LeftTrigger;
            RightTrigger.Value = state.RightTrigger;
            LeftShoulder.Value = state.LeftShoulder;
            RightShoulder.Value = state.RightShoulder;

            A.Value = state.Buttons.A;
            B.Value = state.Buttons.B;
            X.Value = state.Buttons.X;
            Y.Value = state.Buttons.Y;

            DPadLeft.Value = state.DirectionalPad.Left;
            DPadRight.Value = state.DirectionalPad.Right;
            DPadUp.Value = state.DirectionalPad.Up;            
            DPadDown.Value = state.DirectionalPad.Down; 

            //Result.Value = Input1.GetValue(context) + Input2.GetValue(context);
        }

        private SharpDX.XInput.Controller _currentController;

        [Input(Guid = "b37ab751-1e41-4e58-9e27-5ac9052662d8")]
        public readonly InputSlot<int> Index = new();
    }
}
