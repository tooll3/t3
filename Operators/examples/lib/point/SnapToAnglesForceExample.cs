using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.lib.point
{
    [Guid("b64b8677-a6be-4071-b350-249aa3984f93")]
    public class SnapToAnglesForceExample : Instance<SnapToAnglesForceExample>
    {
        [Output(Guid = "089b5a6f-5834-40e9-8b12-b725166ba834")]
        public readonly Slot<Texture2D> TextureOutput = new Slot<Texture2D>();


    }
}

