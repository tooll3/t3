namespace examples.user.ff.pc.elements
{
    [Guid("ffe6b076-f561-4230-a0c3-282fb4d58383")]
    public class PageTitle : Instance<PageTitle>
    {

        [Output(Guid = "3ef7072f-bf0c-4e87-9525-69e19c4ddfdd")]
        public readonly TimeClipSlot<Command> ClipOutput = new();

        [Input(Guid = "409adab7-82e6-4d70-af14-04180a4940b0")]
        public readonly InputSlot<string> Title = new();


    }
}

