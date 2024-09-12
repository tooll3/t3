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
        public readonly InputSlot<System.Numerics.Vector2> Offset = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "2b7af5bb-2ed5-4b9a-89fe-0a45c3cc9111")]
        public readonly InputSlot<float> Amount = new InputSlot<float>();

        [Input(Guid = "4e5c35f7-c815-4959-a3cd-b8f3cddaa9ba")]
        public readonly InputSlot<System.Numerics.Vector3> AmountDistribution = new InputSlot<System.Numerics.Vector3>();

        [Input(Guid = "f4593d23-e4ca-44b0-85e5-4a0038c450a0")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Texture = new InputSlot<SharpDX.Direct3D11.Texture2D>();

        [Input(Guid = "e6bce02b-89be-4a8e-98f8-716576464705")]
        public readonly InputSlot<System.Numerics.Vector2> ScaleUV = new InputSlot<System.Numerics.Vector2>();

        [Input(Guid = "6e005857-0e6d-4c50-9e81-e9339e1acb92")]
        public readonly InputSlot<bool> UseVertexSelection = new InputSlot<bool>();

        [Input(Guid = "e11db6b9-6217-4c76-9ef2-6bd609f0a79a")]
        public readonly InputSlot<SharpDX.Direct3D11.Texture2D> Normal = new InputSlot<SharpDX.Direct3D11.Texture2D>();
        
        private enum Modes
        {
            Surface,
            Surface_XYZ,
            World_XYZ,
        }
    }
}

