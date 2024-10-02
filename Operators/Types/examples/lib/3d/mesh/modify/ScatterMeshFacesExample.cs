using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a35676b8_bcbc_4806_845a_8853b57f8ec7
{
    public class ScatterMeshFacesExample : Instance<ScatterMeshFacesExample>
    {
        [Output(Guid = "6cb9da18-7a64-4722-a19a-882ef894ff5f")]
        public readonly Slot<Texture2D> ImgOutput = new();


    }
}

