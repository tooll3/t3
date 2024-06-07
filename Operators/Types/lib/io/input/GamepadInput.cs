using System;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using SharpDX.DirectInput;
using Vector2 = System.Numerics.Vector2;

namespace T3.Operators.Types.Id_0be5b4ff_1c13_4b07_b2cc_74717c169ec9
{
    public class GamepadInput : Instance<GamepadInput>, IDisposable
    {
       // [Output(Guid = "f6017861-9366-481a-a8eb-a13678f85360", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        //public readonly Slot<bool> Result = new();

        [Output(Guid = "41640d08-18bb-4c2c-875b-64aaec660f1b")]
        public readonly Slot<Vector2> Result = new ();

        public GamepadInput()
        {
            InitializeGamepad();
           // Stick.UpdateAction = Update;
            Result.UpdateAction = Update;
            
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
                Result.Value = new Vector2(0,0);
                return;
            }

            // Poll the gamepad state
            _gamepad.Poll();
            var datas = _gamepad.GetBufferedData();

            // Process button input
            var isDown = false;
            foreach (var data in datas)
            {
                if (data.Offset == JoystickOffset.Buttons0)
                {
                    isDown = data.Value != 0;
                 
                    break;
                }

              // Log.Debug($"{data.Offset}");
            }

            var justPressed = !_wasDown && isDown;
            var justReleased = !isDown && _wasDown;
            _wasDown = isDown;

            // Set the Result based on the mode
           /* switch (Mode.GetValue(context))
            {
                case (int)Modes.Off:
                    Result.Value = false;
                    break;
                case (int)Modes.PressedThisFrame:
                    Result.Value = justPressed;
                    break;
                case (int)Modes.ReleasedThisFrame:
                    Result.Value = justReleased;
                    break;
                case (int)Modes.IsDown:
                    Result.Value = isDown;
                    break;
            }*/

            // Read the stick's X and Y values
            float stickX = 0;
            float stickY = 0;
            foreach (var pos in datas)
            {
                if (pos.Offset == JoystickOffset.X)
                {
                    stickX = (pos.Value / 32767.5f) - 1;
                    Log.Debug("Stick X: " + $"{stickX}");
                }
                else if (pos.Offset == JoystickOffset.Y)
                {
                    stickY = (pos.Value / 32767.5f) - 1;
                    Log.Debug("Stick Y: " + $"{stickY}");
                }
            }

            // Output the stick values
             Result.Value = new Vector2(stickX, stickY);
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
