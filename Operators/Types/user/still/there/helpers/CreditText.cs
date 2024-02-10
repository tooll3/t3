using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_7b1093bb_33ec_4fa4_a102_9a28002b511c
{
    public class CreditText : Instance<CreditText>
    {
        private enum HorizontalAligns
        {
            Left,
            Center,
            Right,
        }
        
        private enum VerticalAligns
        {
            Top,
            Middle,
            Bottom,
        }
        
        [Output(Guid = "58176f40-021a-42a7-b49a-353f1691d3c0")]
        public readonly Slot<Command> Output = new();

        [Input(Guid = "fb727199-d0bb-404e-a671-ec48bc11627b")]
        public readonly InputSlot<string> Input = new();

        [Input(Guid = "e4173f22-ddd9-413c-8ead-88ac71e9fc92")]
        public readonly InputSlot<float> Progress = new();

        [Input(Guid = "c21e5e9c-7e50-41b4-abd2-f054c944221e")]
        public readonly InputSlot<System.Numerics.Vector2> Position = new();

        [Input(Guid = "e0af476f-461d-4d01-b01f-271886269ab6")]
        public readonly InputSlot<int> Style = new();
    }
}

