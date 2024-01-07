using System.Runtime.InteropServices;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace lib.point.combine
{
	[Guid("4dd8a618-eb3b-40af-9851-89c50683d83e")]
    public class CombineBuffers : Instance<CombineBuffers>
    {

        [Output(Guid = "e113f77f-53fe-4b29-95df-2f75e36eb251")]
        public readonly Slot<T3.Core.DataTypes.BufferWithViews> OutBuffer = new();

        [Input(Guid = "b5d25dfd-5d9f-4b5b-b3f5-36b93b13cba3")]
        public readonly MultiInputSlot<T3.Core.DataTypes.BufferWithViews> Input = new();

        [Input(Guid = "a9fd34c5-2583-4014-ab36-ea6c33362d78")]
        public readonly InputSlot<bool> IsEnabled = new();
    }
}

