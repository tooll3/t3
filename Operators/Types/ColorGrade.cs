using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class ColorGrade : Instance<ColorGrade>
    {
        [Input(Guid = "{8FFE42CF-6C2F-4D4E-8892-ADA31451D2B9}")]
        public readonly InputSlot<System.Numerics.Vector4> Color = new InputSlot<System.Numerics.Vector4>();
    }
}