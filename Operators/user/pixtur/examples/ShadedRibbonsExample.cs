using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace user.pixtur.examples
{
	[Guid("4e95a5e8-a075-4493-9aaa-48ea181198e2")]
    public class ShadedRibbonsExample : Instance<ShadedRibbonsExample>
    {
        [Output(Guid = "59996d13-ffca-4817-9514-379ebde296fe")]
        public readonly Slot<Texture2D> ColorBuffer = new();


    }
}

