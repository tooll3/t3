using System.Collections.Generic;
using T3.Core.Operator;

namespace T3.Operators.Types
{
    public class SelectFloatFromList : Instance<SelectFloatFromList>
    {
        [Output(Guid = "{EC2286AF-3EE0-4AF0-AA23-272D4B3710E0}")]
        public readonly Slot<float> Selected = new Slot<float>();

        public SelectFloatFromList()
        {
            Selected.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var list = Input.GetValue(context);
            var index = Index.GetValue(context);
            if (index >= 0 && index < list.Count)
            {
                Selected.Value = list[index];
            }
        }

        [Input(Guid = "{329BA6A4-5B84-43FC-8899-0C04465844DA}")]
        public readonly InputSlot<List<float>> Input = new InputSlot<List<float>>(new List<float>(20));

        [Input(Guid = "{2F87E21A-A1BD-4E2A-948C-2FA35245998D}")]
        public readonly InputSlot<int> Index = new InputSlot<int>(0);
    }
}