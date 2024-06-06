using System;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using SharpDX.DirectInput;

namespace T3.Operators.Types.Id_0be5b4ff_1c13_4b07_b2cc_74717c169ec9
{
    public class GamepadInput : Instance<GamepadInput>, IDisposable
    {
        [Output(Guid = "f6017861-9366-481a-a8eb-a13678f85360", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> Result = new();

        public GamepadInput()
        {
            Result.UpdateAction = Update;
            InitializeGamepad();
        }

        private void InitializeGamepad()
        {
            // Initialize DirectInput
            _directInput = new DirectInput();

            // Find a gamepad
            var gamepadGuid = Guid.Empty;

            foreach (var deviceInstance in _directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices))
            {
                gamepadGuid = deviceInstance.InstanceGuid;
                break; // Use the first available gamepad
            }

            if (gamepadGuid == Guid.Empty)
            {
                Log.Warning("No gamepad found.");
                return;
            }

            // Instantiate the gamepad
            _gamepad = new Joystick(_directInput, gamepadGuid);

            Log.Info($"Found Gamepad with GUID: {gamepadGuid}");

            // Set BufferSize in order to use buffered data
            _gamepad.Properties.BufferSize = 128;

            // Acquire the gamepad
            _gamepad.Acquire();
        }

        private void Update(EvaluationContext context)
        {
            if (_gamepad == null)
            {
                Result.Value = false;
                return;
            }

            // Poll the gamepad state
            _gamepad.Poll();
            var datas = _gamepad.GetBufferedData();

            // Assume keyIndex represents a button index for simplicity
            
            var mode = (Modes)Mode.GetValue(context);

            bool isDown = false;
            foreach (var state in datas)
            {
                if (state.Offset == JoystickOffset.Buttons0 )
                {
                    isDown = state.Value != 0;
                    break;
                }
            }

            var justPressed = !_wasDown && isDown;
            var justReleased = !isDown && _wasDown;
            _wasDown = isDown;

            switch (mode)
            {
                case Modes.Off:
                    Result.Value = false;
                    break;

                case Modes.PressedThisFrame:
                    Result.Value = justPressed;
                    break;

                case Modes.ReleasedThisFrame:
                    Result.Value = justReleased;
                    break;

                case Modes.IsDown:
                    Result.Value = isDown;
                    break;
            }
        }

        private bool _wasDown;
        private DirectInput _directInput;
        private Joystick _gamepad;

        public enum Modes
        {
            Off,
            PressedThisFrame,
            ReleasedThisFrame,
            IsDown,
        }

      

        [Input(Guid = "ce41839a-f451-42eb-a866-2f24a3a9e792", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();

        
    }
}
