using System.Runtime.InteropServices;
using T3.Core.DataTypes;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace examples.user._1x
{
    [Guid("e21c202f-9b93-456d-aeba-7232d9600572")]
    public class HandsOn002BrickOcean : Instance<HandsOn002BrickOcean>
    {

        [Output(Guid = "e0fb287e-3f8e-4387-9fca-09ff034a55c1")]
        public readonly Slot<SharpDX.Direct3D11.Texture2D> Output = new Slot<SharpDX.Direct3D11.Texture2D>();


    }
}

