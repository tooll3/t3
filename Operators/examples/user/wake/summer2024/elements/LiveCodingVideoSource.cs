namespace Types.user.wake.summer2024.elements
{
    [Guid("52282884-fa27-428d-ba8f-eeaf4d69e00a")]
    public class LiveCodingVideoSource : Instance<LiveCodingVideoSource>
    {
        [Output(Guid = "63660f7f-c5ea-4a0c-b155-7cb5d8eab222")]
        public readonly Slot<Command> Output = new Slot<Command>();

        [Input(Guid = "5b561b66-ec20-421b-965f-c17f5c881d8b")]
        public readonly InputSlot<bool> UseNdi = new InputSlot<bool>();

        [Input(Guid = "7bd8f734-23ef-4312-8a22-8f81199ac6b0")]
        public readonly InputSlot<float> FadeIn = new InputSlot<float>();


    }
}

