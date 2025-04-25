using T3.Core.DataTypes;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;
using System.Runtime.InteropServices;

namespace Lib.field.adjust{
    [Guid("e77ac861-5003-4899-a5e0-83059cdde88d")]
    internal sealed class RepeatFieldAtPoints : Instance<RepeatFieldAtPoints>
    {
        [Output(Guid = "202481ea-bf94-4fd3-ad2c-84dbf7622dea")]
        public readonly Slot<ShaderGraphNode> Result = new Slot<ShaderGraphNode>();


        [Input(Guid = "df588d92-76bb-407f-9042-93ddf12e8394")]
        public readonly InputSlot<ShaderGraphNode> InputField = new InputSlot<ShaderGraphNode>();

        [Input(Guid = "9a7f3066-de71-4729-bc9e-5db0d8fd9eaa")]
        public readonly InputSlot<T3.Core.DataTypes.BufferWithViews> Points = new InputSlot<T3.Core.DataTypes.BufferWithViews>();

    }
}

