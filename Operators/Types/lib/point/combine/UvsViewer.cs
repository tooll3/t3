using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_68cf773d_30ac_4ae0_bc1e_b7a17ea322bb
{
    public class UvsViewer : Instance<UvsViewer>
    {

        [Output(Guid = "6ac1d050-592c-4533-9b5e-c9e62884c992")]
        public readonly Slot<T3.Core.DataTypes.MeshBuffers> BlendedMesh = new();

        [Input(Guid = "4ccfb3fe-5c64-45d6-8b3f-63249c69e146")]
        public readonly InputSlot<T3.Core.DataTypes.MeshBuffers> MeshA = new InputSlot<T3.Core.DataTypes.MeshBuffers>();

        [Input(Guid = "017cf5d0-5e39-480e-9a23-ce3d6e1c0d40")]
        public readonly InputSlot<float> BlendValue = new InputSlot<float>();

        [Input(Guid = "09a187f9-1a96-4d1c-84be-522061af7e49")]
        public readonly InputSlot<float> Scatter = new InputSlot<float>();

        [Input(Guid = "0d249c59-d48b-4329-9ad0-fb009e90a700", MappedType = typeof(BlendModes))]
        public readonly InputSlot<int> BlendMode = new InputSlot<int>();

        [Input(Guid = "3efbc6d9-8386-4160-8510-d4dcd4da37d6")]
        public readonly InputSlot<float> RangeWidth = new InputSlot<float>();

        [Input(Guid = "3bb4a7d7-f101-478b-ad99-1bfe3418179e", MappedType = typeof(PairingModes))]
        public readonly InputSlot<int> Pairing = new InputSlot<int>();

        
        private enum BlendModes
        {
            Blend,
            UseW1AsWeight,
            UseW2AsWeight,
            RangeBlend,
            RangeBlendSmooth,
        }
        
        private enum PairingModes
        {
            WrapAround,
            Adjust,
        }
        
    }
} 

