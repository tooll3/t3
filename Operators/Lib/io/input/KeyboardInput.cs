using System.Runtime.InteropServices;
using System;
using T3.Core.IO;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using T3.Core.Logging;
using T3.SystemUi;

namespace lib.io.input
{
	[Guid("2b00bb7a-92cc-41e5-a5f6-bc3e8b16c5eb")]
    public class KeyboardInput : Instance<KeyboardInput>, IDisposable
    {
        [Output(Guid = "55A1258C-3920-4F78-874A-B0652BAC885B", DirtyFlagTrigger = DirtyFlagTrigger.Animated)]
        public readonly Slot<bool> Result = new();

        public KeyboardInput()
        {
            Result.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            var keyIndex = Key.GetValue(context);
            var mode = (Modes)Mode.GetValue(context);

            var isDown = KeyHandler.IsKeyPressed(keyIndex);
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
        
        public enum Modes
        {
            Off,
            PressedThisFrame,
            ReleasedThisFrame,
            IsDown,
        }

        [Input(Guid = "A275FC1A-4036-483E-AF54-6546FA03699C", MappedType = typeof(Key))]
        public readonly InputSlot<int> Key = new();

        [Input(Guid = "73ADAE59-A27D-4D93-8AA6-FB845784BEFD", MappedType = typeof(Modes))]
        public readonly InputSlot<int> Mode = new();
    }
}