using System.Collections.Generic;
// ReSharper disable ConvertToAutoProperty

namespace T3.Core.Operator.Slots
{
    public sealed class MultiInputSlot<T> : InputSlot<T>, IMultiInputSlot
    {
        public List<Slot<T>> CollectedInputs => _collectedInputs;
        private readonly List<Slot<T>> _collectedInputs = new(10);

        public List<Slot<T>> GetCollectedTypedInputs()
        {
            _collectedInputs.Clear();

            foreach (var slot in InputConnections)
            {
                if (slot.TryGetAsMultiInputTyped(out var multiInput) && slot.IsConnected)
                {
                    _collectedInputs.AddRange(multiInput.GetCollectedTypedInputs());
                }
                else
                {
                    _collectedInputs.Add(slot);
                }
            }

            return _collectedInputs;
        }

        public IReadOnlyList<ISlot> GetCollectedInputs()
        {
            return GetCollectedTypedInputs();
        }

        List<int> IMultiInputSlot.LimitMultiInputInvalidationToIndices => LimitMultiInputInvalidationToIndices;
        public readonly List<int> LimitMultiInputInvalidationToIndices = [];

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