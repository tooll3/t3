using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_897a5f40_7970_4770_bd51_08a085f8355b
{
    public class FilesInFolderExample : Instance<FilesInFolderExample>
    {
        [Output(Guid = "5ae5161d-010d-48ab-b0d0-8fc6a1d6e7ce")]
        public readonly Slot<Texture2D> Texture = new();


    }
}

