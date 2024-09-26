using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.@string.list
{
	[Guid("ef357e66-24e9-4f54-8d86-869db74602f4")]
    public class PickFromStringList : Instance<PickFromStringList>
    {
        [Output(Guid = "467bb46e-3391-48a7-b0eb-f7fd9d77b60f")]
        public readonly Slot<string> Selected = new();

        [Output(Guid = "83009BD4-5257-44A2-8091-92B7D2FA5E35")]
        public readonly Slot<int> Count = new();

        
        public PickFromStringList()
        {
            Selected.UpdateAction += Update;
        }

        private void Update(EvaluationContext context)
        {
            var list = Input.GetValue(context);
            if (list == null)
            { 
                Selected.Value= string.Empty;
                Count.Value = 0;
                return;
            }

            var count = list.Count;
            Count.Value = count;
            if (count == 0)
            {
                Selected.Value = string.Empty;
                return;
            }
            
            if (count < 0)
                count = -count;

            var index = Index.GetValue(context) % count;
            if (index >= 0 && index < list.Count)
            {
                Selected.Value = list[index];
            }
        }

        [Input(Guid = "8d5e77a6-1ec4-4979-ad26-f7862049bce1")]
        public readonly InputSlot<List<string>> Input = new(new List<string>(20));

        [Input(Guid = "12ce5fe3-750f-47ed-9507-416cb327a615")]
        public readonly InputSlot<int> Index = new(0);
    }
}