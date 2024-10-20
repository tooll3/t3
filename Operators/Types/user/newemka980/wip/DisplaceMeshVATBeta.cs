using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_17a48029_6144_4c79_80f1_a53c87cadd33
{
    public class DisplaceMeshVATBeta : Instance<DisplaceMeshVATBeta>
    {

        [Output(Guid = "df640cce-fb6f-46fe-962d-ffafd80a954d")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> Result = new();

        [Input(Guid = "e5c52d40-1d93-40a8-b430-868ff4c77107")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> InputMesh = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "480ec4b2-558a-4a64-8a53-55ce1bdcfc18")]
        public readonly InputSlot<System.Numerics.Vector2> UVOffset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "f4593d23-e4ca-44b0-85e5-4a0038c450a0")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "e11db6b9-6217-4c76-9ef2-6bd609f0a79a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Normal = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "bae99b0d-70e5-4f34-a052-5ae133bf9fcb")]
        public readonly InputSlot<bool> RecomputeNormal = new InputSlot<bool>();

        [Input(Guid = "0a07ec19-0623-4240-9354-1b49383fda86")]
        public readonly InputSlot<bool> SplitMesh = new InputSlot<bool>();
        
        private enum Modes
        {
            Surface,
            Surface_XYZ,
            World_XYZ,
        }
    }
}

