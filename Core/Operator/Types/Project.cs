namespace T3.Core.Operator.Types
{
    public class Project : Instance<Project>
    {
        [Output]
        public readonly Slot<string> Output = new Slot<string>("Project Output");



        [FloatInput(DefaultValue = 3.0f)]
        public readonly InputSlot<float> Input = new InputSlot<float>();
    }
}