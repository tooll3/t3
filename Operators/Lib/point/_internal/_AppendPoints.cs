using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_9d3d0582_5e55_4268_9649_07d4dd11d792
{
    public class _AppendPoints : Instance<_AppendPoints>
    {

        [Output(Guid = "02610e60-ae30-46c8-bbab-00ee5b1078d3")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        [Input(Guid = "d331b1f7-3ec3-4dc3-a019-ef72d86b3a98")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GPoints = new();

        [Input(Guid = "8d597942-a0d2-43a0-a039-d450e197702e")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> GTargets = new();
    }
}

