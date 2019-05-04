namespace T3.Core.Operator.Types
{
    public class Project : Instance<Project>
    {
        [Output(Guid = "{3A9734E8-7346-4535-BD54-3B5A735CC6B8}")]
        public readonly Slot<string> Output = new Slot<string>("Project Output");



        [FloatInput(DefaultValue = 3.0f, Guid = "{9BF87C5F-2DFE-482E-8BE7-18FC4D6072E4}")]
        public readonly InputSlot<float> Input = new InputSlot<float>();
    }
}