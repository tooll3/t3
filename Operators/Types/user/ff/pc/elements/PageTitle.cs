using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_ffe6b076_f561_4230_a0c3_282fb4d58383
{
    public class PageTitle : Instance<PageTitle>
    {

        [Output(Guid = "3ef7072f-bf0c-4e87-9525-69e19c4ddfdd")]
        public readonly TimeClipSlot<Command> ClipOutput = new();

        [Input(Guid = "409adab7-82e6-4d70-af14-04180a4940b0")]
        public readonly InputSlot<string> Title = new();


    }
}

