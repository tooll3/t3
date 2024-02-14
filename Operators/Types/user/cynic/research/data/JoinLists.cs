using System.Linq;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_0b99ab8b_5d61_49da_9851_9ad723cad3ae
{
    public class JoinLists : Instance<JoinLists>
    {
        [Output(Guid = "35C71BF9-4636-46CB-BB99-387653755044")]
        public readonly Slot<StructuredList> Result = new();

        
        [Output(Guid = "215CE4F0-8B90-4A4E-8385-B4870DF6A14D")]
        public readonly Slot<int> Length = new();
        
        public JoinLists()
        {
            Length.UpdateAction = Update;
            Result.UpdateAction = Update;
        }


        private void Update(EvaluationContext context)
        {
            var connectedLists = Lists.CollectedInputs.Select(c => c.GetValue(context)).Where(c => c != null).ToList();
            Lists.DirtyFlag.Clear();
            
            if (connectedLists.Count == 0)
            {
                Length.Value = 0;
                return;
            }

            if (connectedLists.Count == 1)
            {
                Result.Value = connectedLists[0].TypedClone();
                Length.Value = 1;
                return;
            }
            
            Result.Value = connectedLists[0].Join(connectedLists.GetRange(1, connectedLists.Count - 1).ToArray());
            Length.Value = Result.Value.NumElements;
        }


        [Input(Guid = "f0eb99fd-5782-4a97-93cb-bed93394184c")]
        public readonly MultiInputSlot<StructuredList> Lists = new();
    }
}