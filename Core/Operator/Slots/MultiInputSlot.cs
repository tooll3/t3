using System.Collections.Generic;

namespace T3.Core.Operator.Slots
{
    public sealed class MultiInputSlot<T> : InputSlot<T>, IMultiInputSlot
    {
        public List<Slot<T>> CollectedInputs { get; } = new(10);

        public List<Slot<T>> GetCollectedTypedInputs()
        {
            CollectedInputs.Clear();

            foreach (var slot in InputConnection)
            {
                if (slot.TryGetAsMultiInputTyped(out var multiInput) && slot.IsConnected)
                {
                    CollectedInputs.AddRange(multiInput.GetCollectedTypedInputs());
                }
                else
                {
                    CollectedInputs.Add(slot);
                }
            }

            return CollectedInputs;
        }

        public IEnumerable<ISlot> GetCollectedInputs()
        {
            return GetCollectedTypedInputs();
        }

        public List<int> LimitMultiInputInvalidationToIndices { get; } = new();

        public void GetValues(ref T[] resources, EvaluationContext context, bool clearDirty= true)
        {
            var connectedInputs = GetCollectedTypedInputs();
            if (connectedInputs.Count != resources.Length)
            {
                resources = new T[connectedInputs.Count];
            }

            for (int i = 0; i < connectedInputs.Count; i++)
            {
                resources[i] = connectedInputs[i].GetValue(context);
            }
            
            if(clearDirty)
                DirtyFlag.Clear();
        }
    }
}